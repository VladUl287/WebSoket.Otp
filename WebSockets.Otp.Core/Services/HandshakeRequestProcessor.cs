using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeRequestProcessor(
    IHandshakeRequestParser handshakeRequestParser,
    IConnectionStateService requestState,
    ITokenIdService tokenIdService,
    ISerializerResolver serializerResolver,
    ILogger<HandshakeRequestProcessor> logger) : IHandshakeRequestProcessor
{
    public bool IsHandshakeRequest(HttpContext ctx, WsMiddlewareOptions options) =>
        ctx.Request.Path.Equals(options.Paths.HandshakePath);

    public async Task HandleRequestAsync(HttpContext ctx, WsMiddlewareOptions options)
    {
        var cancellationToken = ctx.RequestAborted;

        logger.HandshakeRequestStarted(ctx);

        var connectionOptions = await handshakeRequestParser.TryParse(ctx);
        if (connectionOptions is null)
        {
            await ctx.WriteAsync(StatusCodes.Status400BadRequest, "Unable to parse handshake request body", cancellationToken);
            return;
        }

        if (!serializerResolver.Contains(connectionOptions.Protocol))
        {
            await ctx.WriteAsync(StatusCodes.Status400BadRequest, "Protocol not supported", cancellationToken);
            return;
        }

        var tokenId = tokenIdService.Generate();

        await requestState.Set(tokenId, connectionOptions, cancellationToken);

        await ctx.WriteAsync(StatusCodes.Status200OK, tokenId, cancellationToken);

        logger.HandshakeRequestCompleted(ctx);
    }
}
