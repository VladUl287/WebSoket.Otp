namespace WebSockets.Otp.Abstractions;

public abstract class Endpoint
{
    public abstract Task HandleAsync(CancellationToken token);
}

public abstract class Endpoint<TReq> where TReq : class
{
    public abstract Task HandleAsync(TReq request, CancellationToken token);
}
