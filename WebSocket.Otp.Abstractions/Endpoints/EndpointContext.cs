using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Endpoints;

public abstract class EndpointContext(
   IGlobalContext globalContext,
   IWsConnectionManager manager,
   ISerializer serializer,
   IMessageBuffer payload,
   CancellationToken cancellation) : BaseEndpointContext(globalContext, manager, serializer, payload, cancellation)
{
    public SendManager Send => new(ConnectionManager);
}

public abstract class EndpointContext<TResponse>(
    IGlobalContext globalContext,
    IWsConnectionManager manager,
    ISerializer serializer,
    IMessageBuffer payload,
    CancellationToken cancellation) : BaseEndpointContext(globalContext, manager, serializer, payload, cancellation)
    where TResponse : notnull
{
    public SendManager<TResponse> Send => new(ConnectionManager);
}
