using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsRequestProcessor
{
    bool IsWebSocketRequest(HttpContext context, WsMiddlewareOptions options);

    Task HandleWebSocketRequestAsync(HttpContext context, WsMiddlewareOptions options);
}
