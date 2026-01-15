using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public abstract class WsEndpoint
{
    public abstract Task HandleAsync(IWsExecutionContext connection, CancellationToken token);
}

public abstract class WsEndpoint<TRequest>
{
    public abstract Task HandleAsync(TRequest request, IWsExecutionContext context, CancellationToken token);
}
