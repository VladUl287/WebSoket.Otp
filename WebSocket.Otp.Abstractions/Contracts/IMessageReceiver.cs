using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageReceiver
{
    ValueTask<IMessageBuffer> Receive(WebSocket webSocket, WsMiddlewareOptions options, IMessageBuffer buffer, CancellationToken token);
}
