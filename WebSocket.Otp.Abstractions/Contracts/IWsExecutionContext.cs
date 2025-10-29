namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsExecutionContext
{
    string Key { get; }
    IWsConnection Connection { get; }
    CancellationToken Cancellation { get; }
    ISerializer Serializer { get; }
    IMessageBuffer RawPayload { get; }
    Type Endpoint { get; }
}
