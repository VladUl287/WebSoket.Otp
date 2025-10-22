using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Logging;

namespace WebSockets.Otp.AspNet.Middlewares;

public sealed class WsMiddleware(
    RequestDelegate next, IWsService wsService, IConnectionStateService requestState,
    IWsConnectionFactory connectionFactory, IWsAuthorizationService authService, ILogger<WsMiddleware> logger,
    WsMiddlewareOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (IsHandshakeRequest(context, options))
                return HandleHandshakeRequestAsync(context);

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

    private async Task HandleHandshakeRequestAsync(HttpContext context)
    {
        logger.HandshakeRequestStarted(context.Connection.Id);

        if (options is { Authorization.RequireAuthorization: true })
        {
            var authResult = await authService.AuhtorizeAsync(context, options.Authorization);
            if (authResult.Failed)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync(authResult.FailureReason, context.RequestAborted);
                return;
            }
        }

        var connectionOptions = connectionFactory.CreateOptions(context, options);
        var connectionTokenId = await requestState.GenerateTokenId(context, connectionOptions, context.RequestAborted);

        logger.ConnectionTokenGenerated(connectionTokenId, context.Connection.Id);

        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsync(connectionTokenId, context.RequestAborted);
    }

    private async Task HandleWebSocketRequestAsync(HttpContext context)
    {
        var connectionTokenId = connectionFactory.ResolveId(context);
        if (string.IsNullOrEmpty(connectionTokenId))
        {
            logger.MissingConnectionToken(context.Connection.Id);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Missing connection token");
            return;
        }

        options.Connection = await requestState.GetAsync(connectionTokenId, context.RequestAborted);
        if (options is { Connection: null })
        {
            logger.InvalidConnectionToken(connectionTokenId);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid connection token");
            return;
        }

        if (options is { Connection.User: not null })
        {
            context.User = options.Connection.User;
            logger.UserContextSet(options.Connection.User.Identity?.Name ?? "Unknown");
        }

        if (options is { Authorization.RequireAuthorization: true, Connection.User.Identity.IsAuthenticated: false })
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await wsService.HandleWebSocketRequestAsync(context, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsHandshakeRequest(HttpContext context, WsMiddlewareOptions options) =>
        options?.HandshakeRequestMatcher?.IsRequestMatch(context) is true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWebSocketRequest(HttpContext context, WsMiddlewareOptions options) =>
        options?.RequestMatcher?.IsRequestMatch(context) is true;
}
