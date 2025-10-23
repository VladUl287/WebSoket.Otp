using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsService
{
    Task HandleWebSocketRequestAsync(HttpContext context, WsMiddlewareOptions options);
}
