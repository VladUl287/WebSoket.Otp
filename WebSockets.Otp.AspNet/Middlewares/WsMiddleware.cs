using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
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
            var authService = context.RequestServices.GetRequiredService<IWsAuthorizationService>();

            var authResult = authService.AuhtorizeAsync(context, options.Authorization).GetAwaiter().GetResult();
            if (authResult.Failed)
            {
                return HandleAuthorizationFailureAsync(context, authResult.FailureReason);
            }

            if (context is { User.Identity.IsAuthenticated: false })
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return context.Response.WriteAsync("User not authenticated");
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var connectionToken = authService.GenerateConnectionToken(userId);
            context.Response.StatusCode = StatusCodes.Status200OK;
            return context.Response.WriteAsync(connectionToken);
        }

        if (options.RequestMatcher.IsRequestMatch(context))
            return wsService.HandleWebSocketRequestAsync(context, options);

        return next(context);
    }

    private static async Task HandleAuthorizationFailureAsync(HttpContext context, string failureReason)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        await context.Response.WriteAsync(failureReason);
    }
}
