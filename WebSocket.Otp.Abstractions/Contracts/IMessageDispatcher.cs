namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageDispatcher
{
    Task DispatchMessage(IWsContext ctx, ReadOnlyMemory<byte> payload, CancellationToken token = default);
}
