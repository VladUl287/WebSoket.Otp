using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Abstractions.Connections;

public interface IWsConnectionFactory
{
    IWsConnection Create(WebSocket socket, IMessageSerializer serializer);
}
