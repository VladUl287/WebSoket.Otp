using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageDispatcher
{
    Task DispatchMessage(
        IGlobalContext globalContext, ISerializer serializer, IMessageBuffer payload, CancellationToken token);
}
