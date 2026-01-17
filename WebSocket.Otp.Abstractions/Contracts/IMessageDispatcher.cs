using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageDispatcher
{
    Task DispatchMessage(
        IGlobalContext globalContext, ISerializer serializer, IMessageBuffer payload, CancellationToken token);
}
