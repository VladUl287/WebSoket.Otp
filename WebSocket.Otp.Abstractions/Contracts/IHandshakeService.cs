using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Configuration;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeService
{
    ValueTask<WsHandshakeOptions?> ReceiveHandshakeOptions(
        HttpContext context, WebSocket socket, WsBaseConfiguration options, CancellationToken token);
}
