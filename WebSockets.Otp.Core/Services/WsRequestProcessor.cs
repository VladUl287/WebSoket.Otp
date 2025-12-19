using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public sealed class WsRequestProcessor(
    IConnectionStateService requestState,
    IWsConnectionFactory connectionFactory,
    IWsService wsService,
    ILogger<WsRequestProcessor> logger) : IWsRequestProcessor
{
    private const string TextContentType = "text/plain; charset=utf-8";

    public bool IsWebSocketRequest(HttpContext context, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        return IsWsRequestPath(context, options);
    }

    public async Task HandleWebSocketRequestAsync(HttpContext context, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        var cancellationToken = context.RequestAborted;

        if (!IsWsRequestPath(context, options))
        {
            await WriteErrorResponseAsync(context, StatusCodes.Status404NotFound, "Not found", cancellationToken);
            return;
        }

        var connectionTokenId = connectionFactory.GetConnectionTokenId(context);
        if (string.IsNullOrEmpty(connectionTokenId))
        {
            logger.MissingConnectionToken(context.Connection.Id);
            await WriteErrorResponseAsync(context, StatusCodes.Status400BadRequest, "Missing connection token", cancellationToken);
            return;
        }

        var connOptions = await requestState.GetAsync(connectionTokenId, cancellationToken);
        if (connOptions is null)
        {
            logger.InvalidConnectionToken(connectionTokenId);
            await WriteErrorResponseAsync(context, StatusCodes.Status400BadRequest, "Invalid connection token", cancellationToken);
            return;
        }

        options.Connection = connOptions;

        if (options.Connection.User is not null)
        {
            var userName = options.Connection.User.Identity?.Name ?? "Unknown";
            logger.UserContextSet(userName);
            context.User = options.Connection.User;
        }

        if (options is { Authorization.RequireAuthorization: true, Connection.User.Identity.IsAuthenticated: false })
        {
            logger.WebSocketRequestAuthFailed(context.Connection.Id);
            await WriteErrorResponseAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized", cancellationToken);
            return;
        }

        await wsService.HandleWebSocketRequestAsync(context, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWsRequestPath(HttpContext context, WsMiddlewareOptions options)
        => context.WebSockets.IsWebSocketRequest && context.Request.Path.Equals(options.Paths.RequestPath);

    private static Task WriteErrorResponseAsync(HttpContext context, int statusCode, string message, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = TextContentType;
        return context.Response.WriteAsync(message, cancellationToken);
    }
}
