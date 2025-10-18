namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsContext
{
    IWsConnection Connection { get; }

    CancellationToken Cancellation { get; }

    Task SendAsync<T>(T message, CancellationToken token) where T : IWsMessage;
}
