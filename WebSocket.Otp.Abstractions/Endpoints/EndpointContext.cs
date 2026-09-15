using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Endpoints;

public abstract class EndpointContext(
   IGlobalContext context,
   IWsConnectionManager manager,
   ISerializer serializer,
   IMessageBuffer payload,
   CancellationToken token) : BaseEndpointContext(context, manager, serializer, payload, token)
{
    public SendManager Send => new(ConnectionManager);
}

public abstract class EndpointContext<TResponse>(
    IGlobalContext context,
    IWsConnectionManager manager,
    ISerializer serializer,
    IMessageBuffer payload,
    CancellationToken token) : BaseEndpointContext(context, manager, serializer, payload, token)
    where TResponse : notnull
{
    public SendManager<TResponse> Send => new(ConnectionManager);
}
