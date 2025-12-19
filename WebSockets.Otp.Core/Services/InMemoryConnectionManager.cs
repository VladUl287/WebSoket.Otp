using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services;

public sealed class InMemoryConnectionManager : IWsConnectionManager
{
    private readonly ConcurrentDictionary<string, IWsConnection> map = new();

    public bool TryAdd(IWsConnection connection) => map.TryAdd(connection.Id, connection);

    public IWsConnection Get(string connectionId) => map[connectionId];

    public IEnumerable<string> EnumerateIds() => map.Keys.AsEnumerable();

    public bool TryRemove(string connectionId) => map.TryRemove(connectionId, out _);

    public async Task SendAsync(string connectionId, ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        var connection = Get(connectionId);
        await connection.Socket.SendAsync(payload, default, true, token);
    }

    public async Task SendAsync(IEnumerable<string> connectionIds, ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        var connectionIdsArr = connectionIds.ToHashSet();
        foreach (var connection in map.Values.Where(c => connectionIdsArr.Contains(c.Id)))
        {
            await connection.Socket.SendAsync(payload, default, true, token);
        }
    }
}
