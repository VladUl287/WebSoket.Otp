using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class InMemoryConnectionManager : IWsConnectionManager
{
    private readonly ConcurrentDictionary<string, IWsConnection> _store = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IWsConnection>> _groups = new();

    public ValueTask<bool> TryAdd(IWsConnection connection, CancellationToken token) => new(_store.TryAdd(connection.Id, connection));
    public ValueTask<bool> TryRemove(string connectionId, CancellationToken token) => new(_store.TryRemove(connectionId, out _));

    public ValueTask<bool> AddToGroupAsync(string group, string connectionId, CancellationToken token)
    {
        var added = _groups
            .GetOrAdd(group, [])
            .TryAdd(connectionId, _store[connectionId]);
        return ValueTask.FromResult(added);
    }

    public ValueTask<bool> RemoveFromGroupAsync(string group, string connectionId, CancellationToken token)
    {
        var removed = _groups
            .GetOrAdd(group, [])
            .TryRemove(connectionId, out _);
        return ValueTask.FromResult(removed);
    }

    public ValueTask SendAsync<TData>(MessageHeaders headers, TData data, CancellationToken token) where TData : notnull =>
        SendAsync(_store.Values.Select(c => c.Id), headers, data, token);

    public ValueTask SendAsync<TData>(string connectionId, MessageHeaders headers, TData data, CancellationToken token)
        where TData : notnull
    {
        var connection = _store[connectionId];
        var serializer = connection.Serializer;
        var message = serializer.SerializeToMessage(headers, data);
        return connection.Socket.SendAsync(message, serializer.Type, true, token);
    }

    public async ValueTask SendAsync<TData>(
        IEnumerable<string> connections, MessageHeaders headers, TData data, CancellationToken token)
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

            var message = serializer.SerializeToMessage(headers, data);

            tasks.Add(
                Parallel.ForEachAsync(group, options, 
                    (conn, ct) => conn.Socket.SendAsync(message, serializer.Type, true, ct))
            );
        }

        await Task.WhenAll(tasks);
    }

    public async ValueTask SendToGroupAsync<TData>(string group, MessageHeaders headers, TData data, CancellationToken token)
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

            var message = serializer.SerializeToMessage(headers, data);

            tasks.Add(
                Parallel.ForEachAsync(connections, options, 
                    (conn, ct) => conn.Socket.SendAsync(message, serializer.Type, true, ct))
            );
        }

        await Task.WhenAll(tasks);
    }

    public async ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, MessageHeaders headers, TData data, CancellationToken token)
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

            var message = serializer.SerializeToMessage(headers, data);

            tasks.Add(
                Parallel.ForEachAsync(connections, options, 
                    (conn, ct) => conn.Socket.SendAsync(message, serializer.Type, true, ct))
            );
        }

        await Task.WhenAll(tasks);
    }
}
