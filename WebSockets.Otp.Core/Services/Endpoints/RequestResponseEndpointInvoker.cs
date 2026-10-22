using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Models;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class RequestResponseEndpointInvoker<TRequest, TResponse> : IEndpointInvoker
    where TRequest : notnull
    where TResponse : notnull
{
    public async Task Invoke(object endpoint, IEndpointContext context)
    {
        var typedEndpoint = Unsafe.As<WsEndpoint<TRequest, TResponse>>(endpoint);

        var connection = context.Connection;
        var serializer = connection.Serializer;
        var payload = context.Payload;

        var request = serializer.Deserialize<TRequest>(payload.Span) ??
            throw new NullReferenceException(
                $"Deserialization returned null for {typeof(TRequest).Name} at endpoint {endpoint.GetType().Name}.");

        try
        {
            var response = await typedEndpoint.HandleAsync(request, context);

            var responseHeaders = new MessageHeaders { CorrelationId = context.CorrelationId };
            await context.Manager
                .SendAsync(connection.Id, responseHeaders, response, context.Cancellation);
        }
        catch (Exception ex)
        {
            var errorHeaders = new MessageHeaders
            {
                CorrelationId = context.CorrelationId,
                Type = MessageType.Error
            };
            await context.Manager
                .SendAsync(connection.Id, errorHeaders, ex.Message, context.Cancellation);
        }
    }
}
