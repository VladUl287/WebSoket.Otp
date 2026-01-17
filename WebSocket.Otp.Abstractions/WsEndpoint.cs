using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions;

public abstract class WsEndpoint
{
    public abstract Task HandleAsync(EndpointContext connection);
}

public abstract class WsEndpoint<TRequest>
{
    public abstract Task HandleAsync(TRequest request, EndpointContext context);
}

public abstract class WsEndpoint<TRequest, TResponse>
    where TResponse : notnull
{
    public abstract Task HandleAsync(TRequest request, EndpointContext<TResponse> context);
}
