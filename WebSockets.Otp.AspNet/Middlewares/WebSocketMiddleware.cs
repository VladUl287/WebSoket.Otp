using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.AspNet.Middlewares;

public sealed class WebSocketMiddleware: IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await next(context);
    }
}
