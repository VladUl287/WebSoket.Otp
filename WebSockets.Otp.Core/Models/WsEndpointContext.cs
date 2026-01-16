using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Models;

public sealed class WsEndpointContext : EndpointContext
{
}

public sealed class WsEndpointContext<TResponse> : BaseEndpointContext
    where TResponse : notnull
{
}
