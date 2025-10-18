using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.AspNet.Middlewares;

public sealed class WsMiddleware(RequestDelegate next, IWsService wsService, WsMiddlewareOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (options.RequestMatcher.IsWebSocketRequest(context))
            return wsService.HandleWebSocketRequestAsync(context, options);

        return next(context);
    }
}
