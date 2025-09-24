using Microsoft.AspNetCore.Builder;
using WebSockets.Otp.AspNet.Middlewares;
using WebSockets.Otp.AspNet.Options;

namespace WebSockets.Otp.AspNet.Extensions;

public static class WsMiddlewareExtensions
{
    public static IApplicationBuilder UseOtpWebSockets(this IApplicationBuilder builder, Action<WsMiddlewareOptions> configure)
    {
        var options = new WsMiddlewareOptions();
        configure(options);
        return builder.UseMiddleware<WsMiddleware>(options);
    }
}
