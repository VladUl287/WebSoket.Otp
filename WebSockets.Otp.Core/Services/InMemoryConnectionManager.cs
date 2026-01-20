using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services;

public sealed class InMemoryConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, IWsConnection> _store = new();

    public bool TryAdd(IWsConnection connection) => _store.TryAdd(connection.Id, connection);

    public bool TryRemove(string connectionId) => _store.TryRemove(connectionId, out _);

    public ValueTask AddToGroupAsync(string group, string connectionId)
    {
        throw new NotImplementedException();
    }

    public ValueTask RemoveFromGroupAsync(string group, string connectionId)
    {
        throw new NotImplementedException();
    }

    public ValueTask SendAsync<TData>(string connectionId, ReadOnlyMemory<byte> data, CancellationToken token) where TData : notnull
    {
        throw new NotImplementedException();
    }

    public ValueTask SendAsync<TData>(IEnumerable<string> connections, ReadOnlyMemory<byte> data, CancellationToken token) where TData : notnull
    {
        throw new NotImplementedException();
    }

    public ValueTask SendToGroupAsync<TData>(string group, ReadOnlyMemory<byte> data, CancellationToken token) where TData : notnull
    {
        throw new NotImplementedException();
    }

    public ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, ReadOnlyMemory<byte> data, CancellationToken token) where TData : notnull
    {
        throw new NotImplementedException();
    }
}
