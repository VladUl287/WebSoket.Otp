using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class WsExecutionContext(
    string endpointKey, Type endpointType, IWsConnection connection,
    ReadOnlyMemory<byte> rawPayload, ISerializer serializer, CancellationToken cancellation) : IWsExecutionContext
{
    public string Key => endpointKey;
    public IWsConnection Connection => connection;
    public ISerializer Serializer => serializer;
    public ReadOnlyMemory<byte> RawPayload => rawPayload;
    public CancellationToken Cancellation => cancellation;
    public Type Endpoint => endpointType;
}
