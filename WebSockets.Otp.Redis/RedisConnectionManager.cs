using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Connections;

namespace WebSockets.Otp.Redis;

public sealed class RedisConnectionManager : IWsConnectionManager, IAsyncDisposable
{
    private const string DirectChannel = "ws:pubsub:direct";
    private const string GroupChannel = "ws:pubsub:group";
    private const string ConnectionsSetKey = "ws:connections";
    private const string ConnHashPrefix = "ws:connection:";
    private const string GroupSetPrefix = "ws:group:";
    private const string ConnGroupsPrefix = "ws:conn_groups:";

    private sealed record Envelope(string Kind, string[] Targets, string PayloadJson);

    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ISubscriber _sub;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly RedisChannel _directRedisChannel;
    private readonly RedisChannel _groupRedisChannel;

    private readonly ConcurrentDictionary<string, Func<string, ValueTask>> _localHandlers = new();

    public RedisConnectionManager(
        IConnectionMultiplexer redis,
        JsonSerializerOptions? jsonOptions = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _db = redis.GetDatabase();
        _sub = redis.GetSubscriber();

        _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

        _directRedisChannel = RedisChannel.Literal(DirectChannel);
        _groupRedisChannel = RedisChannel.Literal(GroupChannel);

        _sub.Subscribe(_directRedisChannel, (_, msg) => OnPubSubMessageAsync(msg, "direct"));
        _sub.Subscribe(_groupRedisChannel, (_, msg) => OnPubSubMessageAsync(msg, "group"));
    }

    public async ValueTask<bool> TryAdd(IWsConnection connection, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var added = await _db.SetAddAsync(ConnectionsSetKey, connection.Id);
        if (!added) return false;

        var entries = new HashEntry[]
        {
            new("id", connection.Id),
            new("connectedAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        };
        await _db.HashSetAsync(ConnHashPrefix + connection.Id, entries);
        return true;
    }

    public async ValueTask<bool> TryRemove(string connectionId, CancellationToken token)
    {
        if (string.IsNullOrEmpty(connectionId)) return false;

        var groups = await _db.SetMembersAsync(ConnGroupsPrefix + connectionId);
        if (groups.Length > 0)
        {
            var batch = _db.CreateBatch();
            var tasks = new List<Task>(groups.Length);
            foreach (var g in groups)
            {
                var grp = (string)g!;
                tasks.Add(batch.SetRemoveAsync(GroupSetPrefix + grp, connectionId));
            }
            batch.Execute();
            await Task.WhenAll(tasks);
        }

        var removed = await _db.SetRemoveAsync(ConnectionsSetKey, connectionId);

        await Task.WhenAll(
            _db.KeyDeleteAsync(ConnHashPrefix + connectionId),
            _db.KeyDeleteAsync(ConnGroupsPrefix + connectionId));

        return removed;
    }

    public async ValueTask<bool> AddToGroupAsync(string group, string connectionId, CancellationToken token)
    {
        if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(connectionId))
            return false;

        var tran = _db.CreateTransaction();
        _ = tran.SetAddAsync(GroupSetPrefix + group, connectionId);
        _ = tran.SetAddAsync(ConnGroupsPrefix + connectionId, group);
        return await tran.ExecuteAsync();
    }

    public async ValueTask<bool> RemoveFromGroupAsync(string group, string connectionId, CancellationToken token)
    {
        if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(connectionId))
            return false;

        var tran = _db.CreateTransaction();
        _ = tran.SetRemoveAsync(GroupSetPrefix + group, connectionId);
        _ = tran.SetRemoveAsync(ConnGroupsPrefix + connectionId, group);
        return await tran.ExecuteAsync();
    }

    public ValueTask SendAsync<TData>(TData data, CancellationToken token) where TData : notnull
    {
        return BroadcastAsync(data, token);
    }

    public ValueTask SendAsync<TData>(string connectionId, TData data, CancellationToken token)
        where TData : notnull
    {
        if (string.IsNullOrEmpty(connectionId)) return ValueTask.CompletedTask;
        return PublishAsync("direct", [connectionId], data, token);
    }

    public ValueTask SendAsync<TData>(IEnumerable<string> connections, TData data, CancellationToken token)
        where TData : notnull
    {
        var targets = connections?.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray() ?? [];
        if (targets.Length == 0) return ValueTask.CompletedTask;
        return PublishAsync("direct", targets, data, token);
    }

    public ValueTask SendToGroupAsync<TData>(string group, TData data, CancellationToken token)
        where TData : notnull
    {
        if (string.IsNullOrEmpty(group)) return ValueTask.CompletedTask;
        return PublishAsync("group", [group], data, token);
    }

    public ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, TData data, CancellationToken token)
        where TData : notnull
    {
        var targets = groups?.Where(g => !string.IsNullOrEmpty(g)).Distinct().ToArray() ?? [];
        if (targets.Length == 0) return ValueTask.CompletedTask;
        return PublishAsync("group", targets, data, token);
    }

    private async ValueTask BroadcastAsync<TData>(TData data, CancellationToken token) where TData : notnull
    {
        var ids = await _db.SetMembersAsync(ConnectionsSetKey);
        if (ids.Length == 0) return;

        var targets = new string[ids.Length];
        for (int i = 0; i < ids.Length; i++) targets[i] = ids[i]!;
        await PublishAsync("direct", targets, data, token);
    }

    private async ValueTask PublishAsync<TData>(string kind, string[] targets, TData data, CancellationToken token) where TData : notnull
    {
        var payload = JsonSerializer.Serialize(data, _jsonOptions);
        var envelope = new Envelope(kind, targets, payload);
        var json = JsonSerializer.Serialize(envelope, _jsonOptions);

        var channel = kind == "direct" ? _directRedisChannel : _groupRedisChannel;
        await _sub.PublishAsync(channel, json);
    }

    private async Task OnPubSubMessageAsync(RedisValue message, string expectedKind)
    {
        if (message.IsNullOrEmpty) return;

        Envelope? env;
        try
        {
            env = JsonSerializer.Deserialize<Envelope>((string)message!, _jsonOptions);
        }
        catch (JsonException) { return; }

        if (env is null || env.Kind != expectedKind) return;

        foreach (var target in env.Targets)
        {
            if (_localHandlers.TryGetValue(Key(env.Kind, target), out var handler))
            {
                try { await handler(env.PayloadJson); }
                catch { 
                    
                }
            }
        }
    }

    private static string Key(string kind, string target) => kind + ":" + target;

    public void RegisterLocalHandler(string kind, string target, Func<string, ValueTask> handler)
    {
        if (string.IsNullOrEmpty(target) || handler is null) return;
        _localHandlers[Key(kind, target)] = handler;
    }

    public void UnregisterLocalHandler(string kind, string target)
        => _localHandlers.TryRemove(Key(kind, target), out _);

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _sub.UnsubscribeAsync(_directRedisChannel);
            await _sub.UnsubscribeAsync(_groupRedisChannel);
        }
        catch
        {}
    }
}
