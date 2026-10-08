using System.Collections.Concurrent;
using System.Net.WebSockets;
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

    public ValueTask SendAsync(ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken token) =>
        SendAsync(_store.Values.Select(c => c.Id), data, type, token);

    public ValueTask SendAsync(string connectionId, ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken token)
    {
        return _store[connectionId].Socket.SendAsync(data, type, true, token);
    }

    public async ValueTask SendAsync(IEnumerable<string> connections, ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken token)
    {
        foreach (var connection in _store.Where(c => connections.Contains(c.Key)))
        {
            await connection.Value.Socket.SendAsync(data, type, true, token);
        }
    }

    public async ValueTask SendToGroupAsync(string group, ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken token)
    {
        foreach (var connection in _groups[group].Values)
        {
            await connection.Socket.SendAsync(data, type, true, token);
        }
    }

    public async ValueTask SendToGroupAsync(IEnumerable<string> groups, ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken token)
    {
        var groupsStores = _groups
            .Where(group => groups.Contains(group.Key))
            .Select(store => store.Value);

        foreach (var groupStore in groupsStores)
        {
            foreach (var connection in groupStore.Values)
            {
                await connection.Socket.SendAsync(data, type, true, token);
            }
        }
    }
}
