using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions;

public interface IWsEndpoint
{

}

public abstract class WsEndpoint : IWsEndpoint
{
    public abstract Task HandleAsync(EndpointContext context);
}

public abstract class WsEndpoint<TRequest> : IWsEndpoint
{
    public abstract Task HandleAsync(TRequest request, EndpointContext context);
}

public abstract class WsEndpoint<TRequest, TResponse> : IWsEndpoint
    where TResponse : notnull
{
    public abstract Task<TResponse> HandleAsync(TRequest request, EndpointContext context);
}
