using System.Net.WebSockets;

namespace WebSockets.Otp.Abstractions.Connections;

public interface IWsConnectionManager
{
    ValueTask<bool> TryAdd(IWsConnection connection, CancellationToken token);
    ValueTask<bool> TryRemove(string connectionId, CancellationToken token);

    ValueTask<bool> AddToGroupAsync(string group, string connectionId, CancellationToken token);
    ValueTask<bool> RemoveFromGroupAsync(string group, string connectionId, CancellationToken token);

    ValueTask SendAsync(ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken token);
    ValueTask SendAsync(string connectionId, ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken token);
    ValueTask SendAsync(IEnumerable<string> connections, ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken token);

    ValueTask SendToGroupAsync(string group, ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken token);
    ValueTask SendToGroupAsync(IEnumerable<string> groups, ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken token);
}
