using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class MessageEnumeratorFactory(WsConfiguration options) : IMessageEnumeratorFactory
{
    public IMessageEnumerator Create(WebSocket socket) =>
        new MessageEnumerator(socket, options);
}
