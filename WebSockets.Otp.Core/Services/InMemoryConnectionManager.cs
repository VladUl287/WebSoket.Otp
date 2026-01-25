using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Connections;

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

    public ValueTask SendAsync<TData>(TData data, CancellationToken token) where TData : notnull =>
        SendAsync(_store.Values.Select(c => c.Id), data, token);

    public ValueTask SendAsync<TData>(string connectionId, TData data, CancellationToken token)
        where TData : notnull
    {
        var connection = _store[connectionId];
        var socket = connection.Socket;
        var serializer = connection.Serializer;
        var message = serializer.Serialize(data);
        return socket.SendAsync(message, serializer.MessageType, true, token);
    }

    public async ValueTask SendAsync<TData>(IEnumerable<string> connections, TData data, CancellationToken token)
        where TData : notnull
    {
        foreach (var connection in _store.Where(c => connections.Contains(c.Key)))
        {
            var socket = connection.Value.Socket;
            var serializer = connection.Value.Serializer;
            var message = serializer.Serialize(data);
            await socket.SendAsync(message, serializer.MessageType, true, token);
        }
    }

    public async ValueTask SendToGroupAsync<TData>(string group, TData data, CancellationToken token)
        where TData : notnull
    {
        foreach (var connection in _groups[group].Values)
        {
            var socket = connection.Socket;
            var serializer = connection.Serializer;
            var message = serializer.Serialize(data);
            await socket.SendAsync(message, serializer.MessageType, true, token);
        }
    }

    public async ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, TData data, CancellationToken token)
        where TData : notnull
    {
        var groupsStores = _groups
            .Where(group => groups.Contains(group.Key))
            .Select(store => store.Value);

        foreach (var groupStore in groupsStores)
        {
            foreach (var connection in groupStore.Values)
            {
                var socket = connection.Socket;
                var serializer = connection.Serializer;
                var message = serializer.Serialize(data);
                await socket.SendAsync(message, serializer.MessageType, true, token);
            }
        }
    }
}
