namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageDispatcher
{
    Task DispatchMessage(IWsConnection connection, ReadOnlyMemory<byte> payload, CancellationToken token);
}
