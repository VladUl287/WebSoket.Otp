using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeRequestProcessor
{
    bool IsHandshakeRequest(HttpContext context, WsMiddlewareOptions options);

    Task HandleRequestAsync(HttpContext context, WsMiddlewareOptions options);
}
