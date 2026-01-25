using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Abstractions.Connections;

public interface IWsConnection
{
    string Id { get; }
    WebSocket Socket { get; }
    ISerializer Serializer { get; }
}
