using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsService
{
    Task HandleRequestAsync(HttpContext context, WsMiddlewareOptions options, WsConnectionOptions connectionOptions);
}
