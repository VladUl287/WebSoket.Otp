namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnectionManager
{
    bool TryAdd(IWsConnection connection);
    bool TryRemove(string connectionId);

    ValueTask AddToGroupAsync(string group, string connectionId);
    ValueTask RemoveFromGroupAsync(string group, string connectionId);

    ValueTask SendAsync(string connectionId, ReadOnlyMemory<byte> data, CancellationToken token);
    ValueTask SendAsync(IEnumerable<string> connections, ReadOnlyMemory<byte> data, CancellationToken token);
    ValueTask SendToGroupAsync(string group, ReadOnlyMemory<byte> data, CancellationToken token);
    ValueTask SendToGroupAsync(IEnumerable<string> groups, ReadOnlyMemory<byte> data, CancellationToken token);
}
