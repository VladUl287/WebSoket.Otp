using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Core.Models;

public sealed class WsConnection(string connectionId, WebSocket socket, ISerializer serializer) : IWsConnection
{
    public string Id => connectionId;

    public WebSocket Socket => socket;

    public ISerializer Serializer => serializer;
}
