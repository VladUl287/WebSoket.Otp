using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core;

public sealed class HandshakeRequestProcessor(
    IWsAuthorizationService authService,
    IHandshakeRequestParser handshakeRequestParser,
    IConnectionStateService requestState,
    ISerializerFactory serializerFactory,
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

        if (!ValidateConnection(connectionOptions.Protocol, out var statusCode, out var errorMessage))
        {
            await ctx.Response.WriteAsync(statusCode, errorMessage, token);
            return;
        }

        var authorized = await authService.TryAuhtorize(ctx, options.Authorization);
        if (!authorized)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            logger.WebSocketRequestAuthFailed(connectionId);
            return;
        }

        var connectionTokenId = await CreateConnectionAsync(ctx, options, connectionOptions.Protocol, token);
        await ctx.Response.WriteAsync(StatusCodes.Status200OK, connectionTokenId, token);

        logger.HandshakeCompleted(connectionId);
    }

    private bool ValidateConnection(string protocolName, out int statusCode, [NotNullWhen(false)] out string? errorMessage)
    {
        statusCode = 0;
        errorMessage = null;

        var protocol = serializerFactory.Resolve(protocolName);
        if (protocol is null)
        {
            statusCode = StatusCodes.Status400BadRequest;
            errorMessage = $"Protocol '{protocolName}' not supported";
            return false;
        }

        return true;
    }

    private async Task<string> CreateConnectionAsync(HttpContext context, WsMiddlewareOptions options, string protocolName, CancellationToken cancellationToken)
    {
        options.Connection.User = context.User;
        options.Connection.Protocol = protocolName;

        var connectionTokenId = await requestState.GenerateTokenId(context, options.Connection, cancellationToken);

        logger.ConnectionTokenGenerated(connectionTokenId, context.Connection.Id);

        return connectionTokenId;
    }
}
