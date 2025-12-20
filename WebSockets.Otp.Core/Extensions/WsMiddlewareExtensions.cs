using Microsoft.AspNetCore.Builder;
using WebSockets.Otp.Core.Middlewares;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Extensions;

public static class WsMiddlewareExtensions
{
   
    public static IApplicationBuilder UseWsEndpoints(this IApplicationBuilder builder, Action<WsMiddlewareOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var options = new WsMiddlewareOptions();
        configure(options);
        options.Validate();

        return builder.UseMiddleware<WsMiddleware>(options);
    }
}
