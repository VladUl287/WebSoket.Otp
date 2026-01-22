using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class EmptyEndpointInvoker : IEndpointInvoker
{
    public Task Invoke(object endpoint, IEndpointContext context)
    {
        var typedEndpoint = Unsafe.As<WsEndpoint>(endpoint);
        var typedContext = Unsafe.As<EndpointContext>(context);
        return typedEndpoint.HandleAsync(typedContext);
    }
}
