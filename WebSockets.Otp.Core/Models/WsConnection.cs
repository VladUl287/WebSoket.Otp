using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Core.Models;

public sealed class WsConnection(string connectionId, WebSocket socket, ISerializer serializer) : IWsConnection
{
    public string Id => connectionId;

    public WebSocket Socket => socket;

    public ISerializer Serializer => serializer;

    public ValueTask SendAsync<TData>(TData data, CancellationToken token)
    {
        var messageBytes = Serializer.Serialize(data);
        return socket.SendAsync(messageBytes, WebSocketMessageType.Text, true, token);
    }
}
