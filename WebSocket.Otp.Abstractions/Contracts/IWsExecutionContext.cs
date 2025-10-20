namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsExecutionContext
{
    public string Key { get; }
    IWsConnection Connection { get; }
    CancellationToken Cancellation { get; }
    public IMessageSerializer Serializer { get; }
    public ReadOnlyMemory<byte> RawPayload { get; }
    public Type Endpoint { get; }
    public object? RequestMessage { get; set; }
}
