using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public sealed class DefaultHandshakeRequestProcessor(
    IWsAuthorizationService authService,
    IHandshakeRequestParser handshakeRequestParser,
    IConnectionStateService requestState,
    ITokenIdService tokenIdService,
    ISerializerResolver serializerResolver,
    ILogger<DefaultHandshakeRequestProcessor> logger) : IHandshakeRequestProcessor
{
    public bool IsHandshakeRequest(HttpContext ctx, WsMiddlewareOptions options)
    {
        return ctx.Request.Path.Equals(options.Paths.HandshakePath);
    }

    public async Task HandleRequestAsync(HttpContext ctx, WsMiddlewareOptions options)
    {
        var connectionId = ctx.Connection.Id;
        var token = ctx.RequestAborted;

        logger.HandshakeRequestStarted(connectionId);

        var connectionOptions = await handshakeRequestParser.ParseOptions(ctx.Request, token);

        if (!serializerResolver.Registered(connectionOptions.Protocol))
        {
            await ctx.WriteAsync(StatusCodes.Status400BadRequest, "Specified protocol not supported", token);
            return;
        }

        var authorized = await authService.TryAuhtorize(ctx, options.Authorization);
        if (!authorized)
        {
            await ctx.WriteAsync(StatusCodes.Status401Unauthorized, string.Empty, token);
            logger.WebSocketRequestAuthFailed(connectionId);
            return;
        }

        options.Connection.User = ctx.User;
        options.Connection.Protocol = connectionOptions.Protocol;

        var tokenId = tokenIdService.Generate();
        await requestState.Set(tokenId, connectionOptions, token);
        await ctx.WriteAsync(StatusCodes.Status200OK, tokenId, token);

        logger.HandshakeCompleted(connectionId);
    }
}
