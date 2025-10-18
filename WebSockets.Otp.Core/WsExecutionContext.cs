using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class WsExecutionContext(
    string endpointKey, IServiceProvider serviceProvider, IWsConnection connection, 
    ReadOnlyMemory<byte> rawPayload, IMessageSerializer serializer, CancellationToken cancellation) : IWsContext
{
    public string Key => endpointKey;
    public IWsConnection Connection => connection;
    public IServiceProvider RequestServices => serviceProvider;
    public IMessageSerializer Serializer => serializer;
    public ReadOnlyMemory<byte> RawPayload => rawPayload;
    public CancellationToken Cancellation => cancellation;

    public Type? Endpoint { get; set; }
    public object? RequestMessage { get; set; }

    public Task SendAsync<T>(T message, CancellationToken token) where T : IWsMessage
    {
        var bytes = serializer.Serialize(message);
        return connection.SendAsync(bytes, WebSocketMessageType.Text, token);
    }
}