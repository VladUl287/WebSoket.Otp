using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public abstract class WsEndpoint
{
    public abstract Task HandleAsync(EndpointContext connection);
}

public abstract class WsEndpoint<TRequest>
{
    public abstract Task HandleAsync(TRequest request, EndpointContext context);

    //public abstract Task HandleAsync(IEndpointContext context);
    //public abstract Task HandleAsync(TRequest request);
    //public abstract Task HandleAsync(TRequest request, IEndpointContext context);
    //public abstract ValueTask HandleValueAsync(IEndpointContext context);
    //public abstract ValueTask HandleValueAsync(TRequest request);
    //public abstract ValueTask HandleValueAsync(TRequest request, IEndpointContext context);
}

public abstract class WsEndpoint<TRequest, TResponse>
    where TResponse : notnull
{
    public abstract Task HandleAsync(TRequest request, EndpointContext<TResponse> context);
}
