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
        var connectionId = ctx.Connection.Id;

        logger.HandshakeRequestStarted(connectionId);

        var state = await handshakeRequestParser.Deserialize(ctx) ??
            throw new InvalidOperationException();

        if (!serializerResolver.Registered(state.Protocol))
        {
            await ctx.WriteAsync(StatusCodes.Status400BadRequest, "Specified protocol not supported", token);
            return;
        }

        var tokenId = tokenIdService.Generate();

        await requestState.Set(tokenId, state, token);

        await ctx.WriteAsync(StatusCodes.Status200OK, tokenId, token);

        logger.HandshakeCompleted(connectionId);
    }
}
