using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Services.Endpoints.Generic;

public sealed class RequestResponseEndpointInvoker<TRequest, TResponse> : IEndpointInvoker
    where TResponse : notnull
{
    public Task Invoke(object endpoint, IEndpointContext context)
    {
        var typedEndpoint = (WsEndpoint<TRequest, TResponse>)endpoint;
        var typedContext = (EndpointContext<TResponse>)context;

        var request = typedContext.Serializer.Deserialize(typeof(TRequest), typedContext.Payload.Span) ??
            throw new NullReferenceException($"Fail to deserialize message for endpoint '{endpoint.GetType()}'");

        return typedEndpoint.HandleAsync((TRequest)request, typedContext);
    }
}
