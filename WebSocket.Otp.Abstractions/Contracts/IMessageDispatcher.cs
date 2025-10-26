namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageDispatcher
{
    Task DispatchMessage(IWsConnection connection, ISerializer serializer, IMessageBuffer messageBuffer, CancellationToken token);
}
