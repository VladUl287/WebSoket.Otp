using WebSockets.Otp.Abstractions;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class RequestEndpointInvoker<TRequest> : IEndpointInvoker
{
    public Task Invoke(object endpoint, IEndpointContext context)
    {
        var typedEndpoint = Unsafe.As<WsEndpoint<TRequest>>(endpoint);
        var typedContext = Unsafe.As<EndpointContext>(context);

        var request = typedContext.Serializer.Deserialize(typeof(TRequest), typedContext.Payload.Span) ??
            throw new NullReferenceException($"Fail to deserialize message for endpoint '{endpoint.GetType()}'");

        return typedEndpoint.HandleAsync((TRequest)request, typedContext);
    }
}
