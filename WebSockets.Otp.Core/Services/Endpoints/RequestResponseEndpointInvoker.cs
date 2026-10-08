using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class RequestResponseEndpointInvoker<TRequest, TResponse> : IEndpointInvoker
    where TResponse : notnull
{
    public async Task Invoke(object endpoint, IEndpointContext context)
    {
        var typedEndpoint = Unsafe.As<WsEndpoint<TRequest, TResponse>>(endpoint);
        var typedContext = Unsafe.As<EndpointContext>(context);

        var request = typedContext.Serializer.Deserialize<TRequest>(typedContext.Payload.Span) ??
            throw new NullReferenceException($"Fail to deserialize message for endpoint '{endpoint.GetType()}'");

        var response = await typedEndpoint.HandleAsync(request, typedContext);

        var message = typedContext.Serializer.SerializeToMessage(typedContext.Headers, response);

        await typedContext.ConnectionManager.SendAsync(context.ConnectionId, message, context.Cancellation);
    }
}
