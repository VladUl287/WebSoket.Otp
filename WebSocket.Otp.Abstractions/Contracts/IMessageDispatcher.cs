namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageDispatcher
{
    Task DispatchMessage(IWsConnection connection, ReadOnlyMemory<byte> payload, CancellationToken token);
    Task DispatchMessage(IWsConnection connection, IMessageBuffer payload, CancellationToken token);
}
