namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsContext
{
    IWsConnection Connection { get; }

    CancellationToken Cancellation { get; }
}
