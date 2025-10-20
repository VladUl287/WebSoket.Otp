using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.AspNet.Middlewares;

public sealed class WsMiddleware(RequestDelegate next, IWsService wsService, WsMiddlewareOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (options is { Authorization.RequireAuthorization: true } && options.Authorization.RequestMatcher.IsRequestMatch(context))
        {
            if (context is { User.Identity.IsAuthenticated: false })
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return context.Response.WriteAsync("User not authenticated");
            }

            var authorizationService = context.RequestServices.GetRequiredService<IWsAuthorizationService>();
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var connectionToken = authorizationService.GenerateConnectionToken(userId);
            context.Response.StatusCode = StatusCodes.Status200OK;
            return context.Response.WriteAsync(connectionToken);
        }

        if (options.RequestMatcher.IsRequestMatch(context))
            return wsService.HandleWebSocketRequestAsync(context, options);

        return next(context);
    }
}
