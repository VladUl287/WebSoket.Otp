using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Logging;

namespace WebSockets.Otp.AspNet.Middlewares;

public sealed class WsMiddleware(
    RequestDelegate next, IWsService wsService, IRequestState<WsConnectionOptions> requestState,
    IWsConnectionFactory connectionFactory, ILogger<WsMiddleware> logger, WsMiddlewareOptions options)
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
        var connectionOptions = connectionFactory.CreateOptions(context, options);

        var connectionToken = requestState.GenerateKey();
        await requestState.Save(connectionToken, connectionOptions, context.RequestAborted);

        logger.ConnectionTokenGenerated(connectionToken);

        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsync(connectionToken, context.RequestAborted);
    }


    private async Task HandleWebSocketRequestAsync(HttpContext context)
    {
        var connectionOptions = await GetConnectionOptionsAsync(context);
        if (connectionOptions == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid connection token");
            return;
        }

        if (options.Authorization.RequireAuthorization && (context.User?.Identity?.IsAuthenticated != true))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        options.Connection = connectionOptions;
        await wsService.HandleWebSocketRequestAsync(context, options);
    }

    private async Task<WsConnectionOptions?> GetConnectionOptionsAsync(HttpContext context)
    {
        if (!context.Request.Query.TryGetValue("id", out var idValues) || idValues.Count == 0)
            return null;

        var connectionToken = idValues.ToString();
        var connectionOptions = requestState.Get(connectionToken);

        if (connectionOptions?.User != null)
        {
            context.User = connectionOptions.User;
        }

        return connectionOptions;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsHandshakeRequest(HttpContext context, WsMiddlewareOptions options) =>
        options?.HandshakeRequestMatcher?.IsRequestMatch(context) is true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWebSocketRequest(HttpContext context, WsMiddlewareOptions options) =>
        options?.RequestMatcher?.IsRequestMatch(context) is true;
}
