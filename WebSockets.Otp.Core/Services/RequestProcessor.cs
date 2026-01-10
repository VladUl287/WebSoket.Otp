using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class RequestProcessor(
    IWsService wsService,
    IStateService requestState,
    ITokenIdService tokenIdService,
    ILogger<RequestProcessor> logger) : IWsRequestProcessor
{
    public bool IsWebSocketRequest(HttpContext ctx, WsMiddlewareOptions options) =>
        ctx.WebSockets.IsWebSocketRequest && ctx.Request.Path.Equals(options.Paths.RequestPath);

    public async Task HandleRequestAsync(HttpContext ctx, WsMiddlewareOptions options)
    {
        var cancellationToken = ctx.RequestAborted;
        var traceId = new TraceId(ctx);

        logger.WsRequestProcessorStarted(traceId);

        if (!tokenIdService.TryExclude(ctx, out var tokenId))
        {
            logger.WsRequestMissedConnectionToken(traceId);
            await ctx.WriteAsync(StatusCodes.Status400BadRequest, "Missing connection token", cancellationToken);
            return;
        }

        var connectionOptions = await requestState.Get(tokenId, cancellationToken);
        if (connectionOptions is null)
        {
            logger.WsRequestInvalidConnectionToken(traceId, tokenId);
            await ctx.WriteAsync(StatusCodes.Status400BadRequest, "Invalid connection token", cancellationToken);
            return;
        }

        await wsService.HandleRequestAsync(ctx, options, connectionOptions);

        logger.WsRequestProcessorFinished(traceId);
    }
}
