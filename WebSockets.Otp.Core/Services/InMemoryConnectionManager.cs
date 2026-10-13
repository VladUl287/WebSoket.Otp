using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Endpoints;

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

    public ValueTask SendAsync<TData>(EndpointHeaders headers, TData data, CancellationToken token) =>
        SendAsync(_store.Values.Select(c => c.Id), headers, data, token);

    public ValueTask SendAsync<TData>(string connectionId, EndpointHeaders headers, TData data, CancellationToken token)
    {
        var connection = _store[connectionId];
        var serializer = connection.Serializer;
        var message = serializer.SerializeToMessage(headers, data);
        return connection.Socket.SendAsync(message, serializer.Type, true, token);
    }

    public async ValueTask SendAsync<TData>(IEnumerable<string> connections, EndpointHeaders headers, TData data, CancellationToken token)
    {
        var grouped = _store.Values.GroupBy(c => c.Serializer);

        foreach (var group in grouped)
        {
            var serializer = group.Key;
            var message = serializer.SerializeToMessage(headers, data);

            foreach (var connection in group)
            {
                await connection.Socket.SendAsync(message, serializer.Type, true, token);
            }
        }
    }

    public async ValueTask SendToGroupAsync<TData>(string group, EndpointHeaders headers, TData data, CancellationToken token)
    {
        var grouped = _groups[group].Values.GroupBy(c => c.Serializer);

        foreach (var connections in grouped)
        {
            var serializer = connections.Key;
            var message = serializer.SerializeToMessage(headers, data);

            foreach (var connection in connections)
            {
                await connection.Socket.SendAsync(message, serializer.Type, true, token);
            }
        }
    }

    public async ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, EndpointHeaders headers, TData data, CancellationToken token)
    {
        var grouped = _groups
            .Where(c => groups.Contains(c.Key))
            .SelectMany(c => c.Value.Select(c => c.Value))
            .GroupBy(c => c.Serializer);

        foreach (var group in grouped)
        {
            var serializer = group.Key;
            var message = serializer.SerializeToMessage(headers, data);

            foreach (var connection in group)
            {
                await connection.Socket.SendAsync(message, serializer.Type, true, token);
            }
        }
    }
}
