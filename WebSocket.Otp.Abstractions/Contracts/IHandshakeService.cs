using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Configuration;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeService
{
    ValueTask<WsHandshakeOptions?> ReceiveHandshakeOptions(WebSocket socket, CancellationToken token);
}
