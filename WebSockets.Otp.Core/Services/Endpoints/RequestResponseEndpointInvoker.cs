using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;

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

        var request = serializer.Deserialize<TRequest>(payload) ??
            throw new NullReferenceException(
                $"Deserialization returned null for {typeof(TRequest).Name} at endpoint {endpoint.GetType().Name}.");

        try
        {
            var response = await typedEndpoint.HandleAsync(request, context);
            var envelope = new MessageEnvelope<TResponse>
            {
                Id = context.CorrelationId ?? 0,
                Value = response
            };
            await context.Manager.Send(connection.Id, envelope, context.Cancellation);
        }
        catch (Exception ex)
        {
            var envelope = new MessageEnvelope<string>
            {
                Id = context.CorrelationId ?? 0,
                Value = ex.Message,
                Type = MessageType.Error
            };
            await context.Manager.Send(connection.Id, envelope, context.Cancellation);
        }
    }
}
