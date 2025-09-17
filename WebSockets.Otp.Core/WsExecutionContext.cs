using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class WsExecutionContext(
    IServiceProvider sp, IWsConnection conn, ReadOnlyMemory<byte> rawPayload,
    string route, IMessageSerializer serializer, CancellationToken cancellation)
{
    public string Route => route;
    public IWsConnection Connection => conn;
    public IServiceProvider RequestServices => sp;
    public IMessageSerializer Serializer => serializer;

    public Type? Endpoint { get; set; }
    public object? RequestMessage { get; set; }

    public ReadOnlyMemory<byte> RawPayload => rawPayload;

    public CancellationToken Cancellation => cancellation;

    public IWsContext AsPublicContext() => new PublicWsContext(this);

    private sealed class PublicWsContext(WsExecutionContext ec) : IWsContext
    {
        public IWsConnection Connection => ec.Connection;
        public CancellationToken Cancellation => ec.Cancellation;

        public ValueTask SendAsync<T>(T message, CancellationToken token) where T : IWsMessage
        {
            var bytes = ec.Serializer.Serialize(message);
            return ec.Connection.SendAsync(bytes, WebSocketMessageType.Text, token);
        }
    }
}