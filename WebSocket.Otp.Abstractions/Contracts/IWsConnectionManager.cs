namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnectionManager
{
    bool TryAdd(IWsConnection connection);
    bool TryRemove(string connectionId);

    ValueTask AddToGroupAsync(string groupName, string connectionId);
    ValueTask RemoveFromGroupAsync(string groupName, string connectionId);

    ValueTask SendAsync(string connectionId, ReadOnlyMemory<byte> data, CancellationToken token);
}
