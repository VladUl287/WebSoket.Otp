using System.Text.Json;
using StackExchange.Redis;
using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Connections;
using System.Net.WebSockets;

namespace WebSockets.Otp.Redis;

public sealed class RedisConnectionManager : IWsConnectionManager, IAsyncDisposable
{
    private const string DirectChannel = "ws:pubsub:direct";
    private const string GroupChannel = "ws:pubsub:group";
    private const string ConnectionsSetKey = "ws:connections";
    private const string ConnHashPrefix = "ws:connection:";
    private const string GroupSetPrefix = "ws:group:";
    private const string ConnGroupsPrefix = "ws:conn_groups:";

    private sealed record Envelope(string Kind, string[] Targets, string Data, string TypeName);

    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ISubscriber _sub;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly RedisChannel _directRedisChannel;
    private readonly RedisChannel _groupRedisChannel;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IWsConnection>> _localGroups = new();
    private readonly ConcurrentDictionary<string, IWsConnection> _localConnections = new();

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

        _localConnections[connection.Id] = connection;

        await _db.HashSetAsync(ConnHashPrefix + connection.Id,
        [
            new("id", connection.Id),
            new("connectedAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
        ]);

        return true;
    }

    public async ValueTask<bool> TryRemove(string connectionId, CancellationToken token)
    {
        if (string.IsNullOrEmpty(connectionId)) return false;

        var groups = await _db.SetMembersAsync(ConnGroupsPrefix + connectionId);
        if (groups.Length > 0)
        {
            var batch = _db.CreateBatch();
            var tasks = new List<Task>(groups.Length * 2);
            foreach (var g in groups)
            {
                var grp = (string)g!;
                tasks.Add(batch.SetRemoveAsync(GroupSetPrefix + grp, connectionId));
                tasks.Add(batch.SetRemoveAsync(ConnGroupsPrefix + connectionId, grp));

                if (_localGroups.TryGetValue(grp, out var members))
                    members.TryRemove(connectionId, out _);
            }
            batch.Execute();
            await Task.WhenAll(tasks);
        }

        var removed = await _db.SetRemoveAsync(ConnectionsSetKey, connectionId);

        _localConnections.TryRemove(connectionId, out _);

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
        var ok = await tran.ExecuteAsync();

        if (ok && _localConnections.TryGetValue(connectionId, out var conn))
        {
            var members = _localGroups.GetOrAdd(group, _ => new());
            members[connectionId] = conn;
        }

        return ok;
    }

    public async ValueTask<bool> RemoveFromGroupAsync(string group, string connectionId, CancellationToken token)
    {
        if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(connectionId))
            return false;

        var tran = _db.CreateTransaction();
        _ = tran.SetRemoveAsync(GroupSetPrefix + group, connectionId);
        _ = tran.SetRemoveAsync(ConnGroupsPrefix + connectionId, group);
        var ok = await tran.ExecuteAsync();

        if (_localGroups.TryGetValue(group, out var members))
        {
            members.TryRemove(connectionId, out _);

            if (members.IsEmpty)
            {
                _localGroups.TryRemove(group, out _);
            }
        }

        return ok;
    }

    public ValueTask SendAsync<TData>(TData data, CancellationToken token) where TData : notnull
    {
        return BroadcastAsync(data, token);
    }

    public ValueTask SendAsync<TData>(string connectionId, TData data, CancellationToken token)
        where TData : notnull
    {
        if (string.IsNullOrEmpty(connectionId))
            return ValueTask.CompletedTask;

        return PublishAsync("direct", [connectionId], data);
    }

    public ValueTask SendAsync<TData>(IEnumerable<string> connections, TData data, CancellationToken token)
        where TData : notnull
    {
        var targets = connections?.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray() ?? [];
        if (targets.Length == 0)
            return ValueTask.CompletedTask;

        return PublishAsync("direct", targets, data);
    }

    public ValueTask SendToGroupAsync<TData>(string group, TData data, CancellationToken token)
        where TData : notnull
    {
        if (string.IsNullOrEmpty(group))
            return ValueTask.CompletedTask;

        return PublishAsync("group", [group], data);
    }

    public ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, TData data, CancellationToken token)
        where TData : notnull
    {
        var targets = groups?.Where(g => !string.IsNullOrEmpty(g)).Distinct().ToArray() ?? [];
        if (targets.Length == 0)
            return ValueTask.CompletedTask;

        return PublishAsync("group", targets, data);
    }

    private async ValueTask BroadcastAsync<TData>(TData data, CancellationToken token) where TData : notnull
    {
        var ids = await _db.SetMembersAsync(ConnectionsSetKey);
        if (ids.Length == 0) return;

        var targets = new string[ids.Length];
        for (int i = 0; i < ids.Length; i++) targets[i] = ids[i]!;
        await PublishAsync("direct", targets, data);
    }

    private async ValueTask PublishAsync<TData>(string kind, string[] targets, TData data) where TData : notnull
    {
        var payload = JsonSerializer.Serialize(data, _jsonOptions);

        var declaredType = typeof(TData);
        var typeName = declaredType.AssemblyQualifiedName
            ?? declaredType.FullName
            ?? declaredType.Name
            ?? throw new NullReferenceException("");

        var envelope = new Envelope(kind, targets, payload, typeName);
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

        var type = Type.GetType(env.TypeName, throwOnError: false);
        if (type is null) return;

        object? data;
        try
        {
            data = JsonSerializer.Deserialize(env.Data, type, _jsonOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (data is null) return;

        foreach (var target in env.Targets)
        {
            var sockets = env.Kind == "direct"
                ? GetLocalDirect(target)
                : GetLocalGroup(target);

            if (sockets.Count == 0) continue;

            foreach (var group in sockets.GroupBy(c => c.Serializer.Protocol))
            {
                ReadOnlyMemory<byte> bytes;
                var serializer = group.First().Serializer;
                try
                {
                    bytes = serializer.Serialize(data);
                }
                catch
                {
                    continue;
                }

                foreach (var conn in group)
                {
                    try { await conn.Socket.SendAsync(bytes, conn.Serializer.Type, true, CancellationToken.None); }
                    catch
                    { }
                }
            }
        }
    }

    private List<IWsConnection> GetLocalDirect(string connectionId)
        => _localConnections.TryGetValue(connectionId, out var c)
            ? [c]
            : [];

    private List<IWsConnection> GetLocalGroup(string group)
        => _localGroups.TryGetValue(group, out var set)
            ? [.. set.Values]
            : [];

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _sub.UnsubscribeAsync(_directRedisChannel);
            await _sub.UnsubscribeAsync(_groupRedisChannel);
        }
        catch
        { }
    }

    public ValueTask SendAsync(ReadOnlySpan<byte> data, WebSocketMessageType type, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask SendAsync(ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
