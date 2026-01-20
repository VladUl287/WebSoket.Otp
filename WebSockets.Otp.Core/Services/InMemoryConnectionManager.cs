using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Connections;

namespace WebSockets.Otp.Core.Services;

public sealed class InMemoryConnectionManager : IWsConnectionManager
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

    public ValueTask SendAsync<TData>(string connectionId, TData data, CancellationToken token) where TData : notnull
    {
        throw new NotImplementedException();
    }

    public ValueTask SendAsync<TData>(IEnumerable<string> connections, TData data, CancellationToken token) where TData : notnull
    {
        throw new NotImplementedException();
    }

    public ValueTask SendToGroupAsync<TData>(string group, TData data, CancellationToken token) where TData : notnull
    {
        throw new NotImplementedException();
    }

    public ValueTask SendToGroupAsync<TData>(IEnumerable<string> groups, TData data, CancellationToken token) where TData : notnull
    {
        throw new NotImplementedException();
    }
}
