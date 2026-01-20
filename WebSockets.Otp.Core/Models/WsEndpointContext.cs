using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Models;

public sealed class WsEndpointContext : EndpointContext
{
    public WsEndpointContext(
       IGlobalContext globalContext,
       IWsConnectionManager manager,
       ISerializer serializer,
       IMessageBuffer payload,
       CancellationToken cancellation)
        : base(globalContext, manager, serializer, payload, cancellation)
    {
    }
}

public sealed class WsEndpointContext<TResponse> : EndpointContext<TResponse>
    where TResponse : notnull
{
    public WsEndpointContext(
       IGlobalContext globalContext,
       IWsConnectionManager manager,
       ISerializer serializer,
       IMessageBuffer payload,
       CancellationToken cancellation)
        : base(globalContext, manager, serializer, payload, cancellation)
    {
    }
}
