using WebSockets.Otp.Abstractions;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class RequestEndpointInvoker<TRequest> : IEndpointInvoker
    where TRequest : notnull
{
    public Task Invoke(object endpoint, IEndpointContext context)
    {
        var typedEndpoint = Unsafe.As<WsEndpoint<TRequest>>(endpoint);

        var connection = context.Connection;
        var serializer = connection.Serializer;

        var request = serializer.Deserialize<TRequest>(context.Payload.Span) ??
             throw new NullReferenceException(
                $"Deserialization returned null for {typeof(TRequest).Name} at endpoint {endpoint.GetType().Name}.");

        return typedEndpoint.HandleAsync(request, context);
    }
}
