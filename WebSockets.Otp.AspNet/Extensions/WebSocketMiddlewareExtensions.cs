using Microsoft.AspNetCore.Builder;
using WebSockets.Otp.AspNet.Middlewares;

namespace WebSockets.Otp.AspNet.Extensions;

public static class WebSocketMiddlewareExtensions
{
    public static IApplicationBuilder UseOtpWebSockets(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<WebSocketMiddleware>();
    }
}
