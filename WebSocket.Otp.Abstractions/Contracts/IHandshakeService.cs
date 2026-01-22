using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeService
{
    ValueTask<WsHandshakeOptions?> ReceiveHandshakeOptions(
        HttpContext context, WebSocket socket, WsOptions options, CancellationToken token);
}
