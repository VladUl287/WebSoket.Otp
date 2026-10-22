using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions;

public interface IWsEndpoint
{ }

public abstract class WsEndpoint : IWsEndpoint
{
    public abstract Task HandleAsync(IEndpointContext context);
}

public abstract class WsEndpoint<TRequest> : IWsEndpoint
{
    public abstract Task HandleAsync(TRequest request, IEndpointContext context);
}

public abstract class WsEndpoint<TRequest, TResponse> : IWsEndpoint
    where TRequest : notnull
    where TResponse : notnull
{
    public abstract Task<TResponse> HandleAsync(TRequest request, IEndpointContext context);
}
