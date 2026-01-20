using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Endpoints;

public abstract class EndpointContext : BaseEndpointContext
{
    protected EndpointContext(
       IGlobalContext globalContext,
       IWsConnectionManager manager,
       ISerializer serializer,
       IMessageBuffer payload,
       CancellationToken cancellation) : base(globalContext, manager, serializer, payload, cancellation)
    {
    }

    public SendManager Send => new(ConnectionManager);
}

public abstract class EndpointContext<TResponse> : BaseEndpointContext
    where TResponse : notnull
{
    protected EndpointContext(
        IGlobalContext globalContext,
        IWsConnectionManager manager,
        ISerializer serializer,
        IMessageBuffer payload,
        CancellationToken cancellation) : base(globalContext, manager, serializer, payload, cancellation)
    {
    }

    public SendManager<TResponse> Send => new(ConnectionManager);
}
