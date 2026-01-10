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
        var token = ctx.RequestAborted;

        logger.HandshakeRequestStarted(ctx);

        var connectionOptions = await handshakeRequestParser.Deserialize(ctx) ??
            throw new InvalidOperationException();

        if (!serializerResolver.Registered(connectionOptions.Protocol))
        {
            await ctx.WriteAsync(StatusCodes.Status400BadRequest, "Protocol not supported", token);
            return;
        }

        var tokenId = tokenIdService.Generate();

        await requestState.Set(tokenId, connectionOptions, token);

        await ctx.WriteAsync(StatusCodes.Status200OK, tokenId, token);

        logger.HandshakeRequestCompleted(ctx);
    }
}
