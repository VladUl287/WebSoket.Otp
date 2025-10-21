using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.AspNet.Middlewares;

public sealed class WsMiddleware(RequestDelegate next, IWsService wsService, IRequestState<WsConnectionOptions> requestState, WsMiddlewareOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (options.HandshakeRequestMatcher.IsRequestMatch(context))
        {
            var connectionOptions = new WsConnectionOptions();

            if (options is { Authorization.RequireAuthorization: true })
            {
                var authService = context.RequestServices.GetRequiredService<IWsAuthorizationService>();

                var authResult = authService.AuhtorizeAsync(context, options.Authorization).GetAwaiter().GetResult();
                if (authResult.Failed)
                    return HandleAuthorizationFailureAsync(context, authResult.FailureReason);

                connectionOptions.User = context.User;
            }

            connectionOptions.Protocol = "JSON";

            var connectionToken = requestState.GenerateKey();

            requestState.Save(connectionToken, connectionOptions);

            context.Response.StatusCode = StatusCodes.Status200OK;
            return context.Response.WriteAsync(connectionToken);
        }

        if (options.RequestMatcher.IsRequestMatch(context))
        {
            if (context.Request.Query["id"].Any())
            {
                var connectionToken = context.Request.Query["id"];
                var state = requestState.Get(connectionToken);
                if (state is { User: not null})
                {
                    context.User = state.User;
                }
            }

            if (options is { Authorization.RequireAuthorization: true } && context is { User.Identity.IsAuthenticated: false })
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return context.Response.WriteAsync("");
            }

            return wsService.HandleWebSocketRequestAsync(context, options);
        }

        return next(context);
    }

    private static async Task HandleAuthorizationFailureAsync(HttpContext context, string failureReason)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        await context.Response.WriteAsync(failureReason);
    }
}
