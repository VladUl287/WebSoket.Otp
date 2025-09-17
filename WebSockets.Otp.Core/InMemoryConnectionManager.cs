using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class InMemoryConnectionManager : IWsConnectionManager
{
    private readonly ConcurrentDictionary<string, IWsConnection> map = new();

    public bool TryAdd(IWsConnection connection) => map.TryAdd(connection.Id, connection);

    public IWsConnection Get(string connectionId) => map[connectionId];

    public IEnumerable<IWsConnection> GetAll() => map.Values;

    public bool TryRemove(string connectionId) => map.TryRemove(connectionId, out _);
}
