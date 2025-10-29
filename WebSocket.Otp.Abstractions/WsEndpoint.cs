using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public abstract class WsEndpoint
{
    public abstract Task HandleAsync(IWsExecutionContext connection, CancellationToken token);
}

public abstract class WsEndpoint<TReq> where TReq : WsMessage
{
    public abstract Task HandleAsync(TReq request, IWsExecutionContext context, CancellationToken token);
}
