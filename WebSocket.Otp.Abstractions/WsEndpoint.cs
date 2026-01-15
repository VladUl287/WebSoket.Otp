using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public abstract class WsEndpoint
{
    public abstract Task HandleAsync(IEndpointExecutionContext connection, CancellationToken token);
}

public abstract class WsEndpoint<TRequest>
{
    public abstract Task HandleAsync(TRequest request, IEndpointExecutionContext context, CancellationToken token);
}
