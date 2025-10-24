using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Logging;

namespace WebSockets.Otp.AspNet;

public sealed record HandshakeRequest(string Protocol);

public sealed class HandshakeRequestProcessor(
    IWsAuthorizationService authService,
    IConnectionStateService requestState,
    ILogger<HandshakeRequestProcessor> logger) : IHandshakeRequestProcessor
{
    public bool IsHandshakeRequest(HttpContext context, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        return context.Request.Path.Equals(options.Paths.HandshakePath);
    }

    public async Task HandleRequestAsync(HttpContext context, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var connectionId = context.Connection.Id;

        logger.HandshakeRequestStarted(connectionId);

        if (!HttpMethods.IsPost(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("", context.RequestAborted);
            return;
        }

        if (context.Request.ContentType?.Contains("application/json") is false)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("", context.RequestAborted);
            return;
        }

        var body = await JsonSerializer.DeserializeAsync<HandshakeRequest>(context.Request.Body);
        if (body == null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("", context.RequestAborted);
            return;
        }

        var registered = context.RequestServices.GetRequiredService<ISerializerFactory>();
        var protocol = registered.Create(body.Protocol);
        if (protocol is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Protocol not registered", context.RequestAborted);
            return;
        }

        var authResult = await authService.AuhtorizeAsync(context, options.Authorization);
        if (authResult.Failed)
        {
            logger.WebSocketRequestAuthFailed(connectionId);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(authResult.FailureReason, context.RequestAborted);
            return;
        }

        var connectionOptions = options.Connection;
        connectionOptions.User = context.User;
        connectionOptions.Protocol = WsProtocol.New(body.Protocol);

        var connectionTokenId = await requestState.GenerateTokenId(context, connectionOptions, context.RequestAborted);

        logger.ConnectionTokenGenerated(connectionTokenId, connectionId);

        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsync(connectionTokenId, context.RequestAborted);

        logger.HandshakeCompleted(connectionId);
    }
}
