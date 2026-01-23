using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeHandler
{
    ValueTask<WsHandshakeOptions?> HandleAsync(
        HttpContext context, WebSocket socket, WsConfiguration config, CancellationToken token);
}
