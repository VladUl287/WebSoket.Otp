namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsExecutionContext
{
    public string Key { get; }
    IWsConnection Connection { get; }
    CancellationToken Cancellation { get; }
    public ISerializer Serializer { get; }
    public IMessageBuffer RawPayload { get; }
    public Type Endpoint { get; }
}
