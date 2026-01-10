using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public sealed class WsRequestProcessor(
    IConnectionStateService requestState,
    ITokenIdService tokenIdService,
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

    public async Task HandleWebSocketRequestAsync(HttpContext ctx, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(options);

        var token = ctx.RequestAborted;

        if (!IsWsRequestPath(ctx, options))
        {
            await ctx.WriteAsync(StatusCodes.Status404NotFound, "Not found", token);
            return;
        }

        if (!tokenIdService.TryExclude(ctx.Request, out var tokenId))
        {
            logger.MissingConnectionToken(ctx.Connection.Id);
            await ctx.WriteAsync(StatusCodes.Status400BadRequest, "Missing connection token", token);
            return;
        }

        var connOptions = await requestState.Get(tokenId, token);
        if (connOptions is null)
        {
            logger.InvalidConnectionToken(tokenId);
            await ctx.WriteAsync(StatusCodes.Status400BadRequest, "Invalid connection token", token);
            return;
        }

        options.Connection = connOptions;

        if (options.Connection.User is not null)
        {
            var userName = options.Connection.User.Identity?.Name ?? "Unknown";
            logger.UserContextSet(userName);
            ctx.User = options.Connection.User;
        }

        if (options is { Authorization.RequireAuthorization: true, Connection.User.Identity.IsAuthenticated: false })
        {
            logger.WebSocketRequestAuthFailed(ctx.Connection.Id);
            await ctx.WriteAsync(StatusCodes.Status401Unauthorized, "Unauthorized", token);
            return;
        }

        await wsService.HandleWebSocketRequestAsync(ctx, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWsRequestPath(HttpContext context, WsMiddlewareOptions options)
        => context.WebSockets.IsWebSocketRequest && context.Request.Path.Equals(options.Paths.RequestPath);
}
