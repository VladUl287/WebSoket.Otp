using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public sealed class RequestProcessor(
    IStateService requestState,
    ITokenIdService tokenIdService,
    IWsService wsService,
    ILogger<RequestProcessor> logger) : IWsRequestProcessor
{
    public bool IsWebSocketRequest(HttpContext ctx, WsMiddlewareOptions options) =>
        ctx.WebSockets.IsWebSocketRequest && ctx.Request.Path.Equals(options.Paths.RequestPath);

    public async Task HandleRequestAsync(HttpContext ctx, WsMiddlewareOptions options)
    {
        var cancellationToken = ctx.RequestAborted;

        if (!tokenIdService.TryExclude(ctx, out var tokenId))
        {
            logger.MissingConnectionToken(ctx.Connection.Id);
            await ctx.WriteAsync(StatusCodes.Status400BadRequest, "Missing connection token", cancellationToken);
            return;
        }

        var connectionOptions = await requestState.Get(tokenId, cancellationToken);
        if (connectionOptions is null)
        {
            logger.InvalidConnectionToken(tokenId);
            await ctx.WriteAsync(StatusCodes.Status400BadRequest, "Invalid connection token", cancellationToken);
            return;
        }

        await wsService.HandleRequestAsync(ctx, options, connectionOptions);
    }
}
