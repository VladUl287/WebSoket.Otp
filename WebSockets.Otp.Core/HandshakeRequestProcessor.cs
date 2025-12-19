using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core;

public sealed class HandshakeRequestProcessor(
    IWsAuthorizationService authService,
    IHandshakeRequestParser handshakeRequestParser,
    IConnectionStateService requestState,
    ISerializerResolver serializerResolver,
    ILogger<HandshakeRequestProcessor> logger) : IHandshakeRequestProcessor
{
    public bool IsHandshakeRequest(HttpContext ctx, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        return ctx.Request.Path.Equals(options.Paths.HandshakePath);
    }

    public async Task HandleRequestAsync(HttpContext ctx, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var connectionId = ctx.Connection.Id;
        var token = ctx.RequestAborted;

        logger.HandshakeRequestStarted(connectionId);

        var connectionOptions = await handshakeRequestParser.ParseOptions(ctx.Request, token);

        if (!serializerResolver.Registered(connectionOptions.Protocol))
        {
            await ctx.Response.WriteAsync(StatusCodes.Status400BadRequest, "Specified protocol not supported", token);
            //log
            return;
        }

        var authorized = await authService.TryAuhtorize(ctx, options.Authorization);
        if (!authorized)
        {
            await ctx.Response.WriteAsync(StatusCodes.Status401Unauthorized, string.Empty, token);
            logger.WebSocketRequestAuthFailed(connectionId);
            return;
        }

        options.Connection.User = ctx.User;
        options.Connection.Protocol = connectionOptions.Protocol;

        var tokenId = await requestState.GenerateTokenId(ctx, options.Connection, token);
        await ctx.Response.WriteAsync(StatusCodes.Status200OK, tokenId, token);

        logger.HandshakeCompleted(connectionId);
    }
}
