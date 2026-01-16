using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public abstract class WsEndpoint
{
    public abstract Task HandleAsync(IEndpointContext connection);
}

public abstract class WsEndpoint<TRequest>
{
    public abstract Task HandleAsync(TRequest request, IEndpointContext context);
}

public abstract class WsEndpoint<TRequest, TResponse>
    where TResponse : notnull
{
    public abstract Task HandleAsync(TRequest request, IEndpointContext<TResponse> context);
}
