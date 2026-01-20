using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services;

public sealed class InMemoryConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, IWsConnection> _store = new();

    public bool TryAdd(IWsConnection connection) => _store.TryAdd(connection.Id, connection);

    public bool TryRemove(string connectionId) => _store.TryRemove(connectionId, out _);

    public IEnumerable<string> All() => _store.Keys.AsEnumerable();

    public IEnumerable<string> All(string groupName) => _store.Keys.AsEnumerable();

    public IWsConnection Get(string connectionId) => _store[connectionId];

    public ValueTask AddToGroupAsync(string connectionId, string groupName)
    {
        throw new NotImplementedException();
    }

    public ValueTask RemoveFromGroupAsync(string connectionId, string groupName)
    {
        throw new NotImplementedException();
    }

    public ValueTask SendAsync(string connectionId, ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask SendAsync(IEnumerable<string> connectionIds, ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask SendToGroupAsync(string group, ReadOnlyMemory<byte> data, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ValueTask SendToGroupAsync(IEnumerable<string> groups, ReadOnlyMemory<byte> data, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
