using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Logging;

namespace WebSockets.Otp.AspNet.Middlewares;

public sealed class WsMiddleware(
    RequestDelegate next, IWsService wsService, IConnectionStateService requestState,
    IWsConnectionFactory connectionFactory, ILogger<WsMiddleware> logger,
    IHandshakeRequestProcessor handshakeProcessor, WsMiddlewareOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (handshakeProcessor.IsHandshakeRequest(context, options))
                return handshakeProcessor.HandleRequestAsync(context, options);

            if (IsWebSocketRequest(context, options))
                return HandleWebSocketRequestAsync(context);

            return next(context);
        }
        catch (Exception ex)
        {
            logger.WsMiddlewareError(ex);
            throw;
        }
    }

    private async Task HandleWebSocketRequestAsync(HttpContext context)
    {
        var connectionTokenId = connectionFactory.GetConnectionTokenId(context);
        if (string.IsNullOrEmpty(connectionTokenId))
        {
            logger.MissingConnectionToken(context.Connection.Id);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Missing connection token");
            return;
        }

        var connOptions = await requestState.GetAsync(connectionTokenId, context.RequestAborted);
        if (connOptions is null)
        {
            logger.InvalidConnectionToken(connectionTokenId);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid connection token");
            return;
        }

        options.Connection = connOptions;

        if (options is { Connection.User: not null })
        {
            logger.UserContextSet(options.Connection.User.Identity?.Name ?? "Unknown");

            context.User = options.Connection.User;
        }

        if (options is { Authorization.RequireAuthorization: true, Connection.User.Identity.IsAuthenticated: false })
        {
            logger.WebSocketRequestAuthFailed(context.Connection.Id);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await wsService.HandleWebSocketRequestAsync(context, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWebSocketRequest(HttpContext context, WsMiddlewareOptions options) =>
         options?.Paths.RequestMatcher?.IsRequestMatch(context) is true;
}
