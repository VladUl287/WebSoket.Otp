using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public abstract class WsEndpoint : IEndpoint
{
    public abstract Task HandleAsync(IWsContext connection, CancellationToken token);
}

public abstract class WsEndpoint<TReq> : IEndpoint where TReq : IMessage
{
    public abstract Task HandleAsync(TReq request, IWsContext context, CancellationToken token);
}
