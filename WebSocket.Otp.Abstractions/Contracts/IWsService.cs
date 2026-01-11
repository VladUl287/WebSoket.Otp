using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsService
{
    Task HandleRequestAsync(HttpContext context, WsMiddlewareOptions options);
    Task HandleRequestAsync(ConnectionContext context, WsMiddlewareOptions options);
}
