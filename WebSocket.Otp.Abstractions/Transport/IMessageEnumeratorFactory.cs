using System.Net.WebSockets;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageEnumeratorFactory
{
    IMessageEnumerator Create(WebSocket socket);
}
