using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public abstract class WsEndpoint : IWsEndpoint
{
    public abstract Task HandleAsync(IWsExecutionContext connection, CancellationToken token);
}

public abstract class WsEndpoint<TReq> : IWsEndpoint where TReq : IWsMessage
{
    public abstract Task HandleAsync(TReq request, IWsExecutionContext context, CancellationToken token);
}
