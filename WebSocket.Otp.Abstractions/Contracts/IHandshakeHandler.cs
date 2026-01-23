using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeHandler
{
    ValueTask<WsHandshakeOptions?> HandleAsync(
        HttpContext context, WebSocket socket, WsConfiguration config, CancellationToken token);
}
