namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnectionManager
{
    bool TryAdd(IWsConnection connection);

    bool TryRemove(string connectionId);

    IWsConnection Get(string connectionId);

    IEnumerable<string> EnumerateIds();

    Task SendAsync(string connectionId, ReadOnlyMemory<byte> payload, CancellationToken token);

    Task SendAsync(IEnumerable<string> connectionIds, ReadOnlyMemory<byte> payload, CancellationToken token);
}
