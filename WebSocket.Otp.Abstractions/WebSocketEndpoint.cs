using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public abstract class WebSocketEndpoint : IEndpoint
{
    public abstract Task HandleAsync(CancellationToken token);
}

public abstract class WebSocketEndpoint<TReq> : IEndpoint where TReq : class
{
    public abstract Task HandleAsync(TReq request, CancellationToken token);
}
