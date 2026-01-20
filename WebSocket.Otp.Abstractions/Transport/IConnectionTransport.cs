namespace WebSockets.Otp.Abstractions.Transport;

public interface IConnectionTransport
{
    ValueTask SendAsync<TData>(TData data, CancellationToken token)
        where TData : notnull;
}
