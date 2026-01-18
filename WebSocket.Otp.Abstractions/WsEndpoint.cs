using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions;

public interface IWsEndpoint
{

}

public abstract class BaseWsEndpoint : IWsEndpoint
{

}

public abstract class WsEndpoint : BaseWsEndpoint
{
    public abstract Task HandleAsync(EndpointContext connection);
}

public abstract class WsEndpoint<TRequest> : BaseWsEndpoint
{
    public abstract Task HandleAsync(TRequest request, EndpointContext context);
}

public abstract class WsEndpoint<TRequest, TResponse> : BaseWsEndpoint
    where TResponse : notnull
{
    public abstract Task HandleAsync(TRequest request, EndpointContext<TResponse> context);
}
