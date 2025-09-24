using Microsoft.AspNetCore.Http;
using WebSockets.Otp.AspNet.Options;

namespace WebSockets.Otp.AspNet.Middlewares;

public sealed class WsMiddleware(RequestDelegate next, WsMiddlewareOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);
    }
}
