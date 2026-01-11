using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsRequestProcessor
{
    Task HandleRequestAsync(HttpContext context, WsMiddlewareOptions options);
}
