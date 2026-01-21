using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Services.Endpoints.Generic;

public sealed class EmptyEndpointInvoker : IEndpointInvoker
{
    public Task Invoke(object endpoint, IEndpointContext context)
    {
        var typedEndpoint = (WsEndpoint)endpoint;
        var typedContext = (EndpointContext)context;
        return typedEndpoint.HandleAsync(typedContext);
    }
}
