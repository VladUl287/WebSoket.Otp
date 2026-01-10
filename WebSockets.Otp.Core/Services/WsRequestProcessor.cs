using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
    public bool IsWebSocketRequest(HttpContext ctx, WsMiddlewareOptions options) =>
        ctx.WebSockets.IsWebSocketRequest && ctx.Request.Path.Equals(options.Paths.RequestPath);

    public async Task HandleRequestAsync(HttpContext ctx, WsMiddlewareOptions options)
    {
        var token = ctx.RequestAborted;

        if (!tokenIdService.TryExclude(ctx, out var tokenId))
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

        if (options is { Authorization.RequireAuthorization: true } && ctx is { User.Identity.IsAuthenticated: false })
        {
            logger.WebSocketRequestAuthFailed(ctx.Connection.Id);
            await ctx.WriteAsync(StatusCodes.Status401Unauthorized, "Unauthorized", token);
            return;
        }

        await wsService.HandleRequestAsync(ctx, options, connOptions);
    }
}
