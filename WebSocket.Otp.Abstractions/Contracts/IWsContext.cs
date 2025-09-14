namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsContext
{
    IWsConnection Connection { get; }

    CancellationToken Cancellation { get; }

    ValueTask SendAsync<T>(T message, CancellationToken token) where T : IMessage;
}
