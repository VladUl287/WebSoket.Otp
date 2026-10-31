using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services;

public sealed class InMemoryConnectionManager : IWsConnectionManager
{
    private readonly ConcurrentDictionary<string, IWsConnection> _store = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IWsConnection>> _groups = new();

    public ValueTask<bool> TryAdd(IWsConnection connection, CancellationToken token) => new(_store.TryAdd(connection.Id, connection));
    public ValueTask<bool> TryRemove(string connectionId, CancellationToken token) => new(_store.TryRemove(connectionId, out _));

    public ValueTask<bool> AddToGroup(string group, string connectionId, CancellationToken token)
    {
        var added = _groups
            .GetOrAdd(group, [])
            .TryAdd(connectionId, _store[connectionId]);
        return ValueTask.FromResult(added);
    }

    public ValueTask<bool> RemoveFromGroup(string group, string connectionId, CancellationToken token)
    {
        var removed = _groups
            .GetOrAdd(group, [])
            .TryRemove(connectionId, out _);
        return ValueTask.FromResult(removed);
    }

    public ValueTask Send<TData>(MessageEnvelope<TData> envelope, CancellationToken token) where TData : notnull =>
        Send(_store.Values.Select(c => c.Id), envelope, token);

    public ValueTask Send<TData>(string connectionId, MessageEnvelope<TData> envelope, CancellationToken token)
        where TData : notnull
    {
        var connection = _store[connectionId];
        var serializer = connection.Serializer;
        var message = serializer.SerializeToMessage(envelope);
        return connection.Socket.SendAsync(message, serializer.Type, true, token);
    }

    public async ValueTask Send<TData>(
        IEnumerable<string> connections, MessageEnvelope<TData> envelope, CancellationToken token)
        where TData : notnull
    {
        var requested = new HashSet<string>(connections);

        var grouped = _store.Values
            .Where(c => requested.Contains(c.Id))
            .GroupBy(c => c.Serializer);

        var tasks = new List<Task>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = token
        };

        foreach (var group in grouped)
        {
            var serializer = group.Key;

            var message = serializer.SerializeToMessage(envelope);

            tasks.Add(
                Parallel.ForEachAsync(group, options, 
                    (conn, ct) => conn.Socket.SendAsync(message, serializer.Type, true, ct))
            );
        }

        await Task.WhenAll(tasks);
    }

    public async ValueTask SendToGroup<TData>(string group, MessageEnvelope<TData> envelope, CancellationToken token)
        where TData : notnull
    {
        var grouped = _groups[group].Values
            .GroupBy(c => c.Serializer);

        var tasks = new List<Task>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = token
        };

        foreach (var connections in grouped)
        {
            var serializer = connections.Key;

            var message = serializer.SerializeToMessage(envelope);

            tasks.Add(
                Parallel.ForEachAsync(connections, options, 
                    (conn, ct) => conn.Socket.SendAsync(message, serializer.Type, true, ct))
            );
        }

        await Task.WhenAll(tasks);
    }

    public async ValueTask SendToGroup<TData>(IEnumerable<string> groups, MessageEnvelope<TData> envelope, CancellationToken token)
        where TData : notnull
    {
        var requested = new HashSet<string>(groups);

        var grouped = _groups
            .Where(c => requested.Contains(c.Key))
            .SelectMany(c => c.Value.Select(c => c.Value))
            .GroupBy(c => c.Serializer);

        var tasks = new List<Task>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = token
        };

        foreach (var connections in grouped)
        {
            var serializer = connections.Key;

            var message = serializer.SerializeToMessage(envelope);

            tasks.Add(
                Parallel.ForEachAsync(connections, options, 
                    (conn, ct) => conn.Socket.SendAsync(message, serializer.Type, true, ct))
            );
        }

        await Task.WhenAll(tasks);
    }
}
