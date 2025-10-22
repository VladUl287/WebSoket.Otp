using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnectionFactory
{
    IWsConnection Create(HttpContext context, WebSocket socket);
    WsConnectionOptions CreateOptions(HttpContext context, WsMiddlewareOptions options);
    string GetConnectionToken(HttpContext context);
}
