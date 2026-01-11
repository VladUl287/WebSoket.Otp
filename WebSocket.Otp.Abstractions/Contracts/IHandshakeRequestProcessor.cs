using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeRequestProcessor
{
    Task HandleRequestAsync(HttpContext context, WsMiddlewareOptions options);
}
