using Microsoft.Extensions.ObjectPool;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class WsExecutionContext(
    string endpointKey, Type endpointType, IWsConnection connection,
    IMessageBuffer rawPayload, ISerializer serializer, CancellationToken cancellation) : IWsExecutionContext
{
    public string Key => endpointKey;
    public IWsConnection Connection => connection;
    public ISerializer Serializer => serializer;
    public IMessageBuffer RawPayload => rawPayload;
    public CancellationToken Cancellation => cancellation;
    public Type Endpoint => endpointType;
}