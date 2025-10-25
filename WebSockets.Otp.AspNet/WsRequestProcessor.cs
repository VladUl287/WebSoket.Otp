using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Logging;

namespace WebSockets.Otp.AspNet;

public sealed class WsRequestProcessor(
    IConnectionStateService requestState, IWsConnectionFactory connectionFactory, IWsService wsService,
    ILogger<WsRequestProcessor> logger) : IWsRequestProcessor
{
    public bool IsWebSocketRequest(HttpContext context, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        return IsWsRequestPath(context, options);
    }

    public async Task HandleWebSocketRequestAsync(HttpContext context, WsMiddlewareOptions options)
    {
        var cancellationToken = context.RequestAborted;

        if (!IsWsRequestPath(context, options))
        {
            //log
            await context.Response.WriteAsync("", cancellationToken);
            return;
        }

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
    private static bool IsWsRequestPath(HttpContext context, WsMiddlewareOptions options) =>
        context.WebSockets.IsWebSocketRequest && context.Request.Path.Equals(options.Paths.RequestPath);
}
