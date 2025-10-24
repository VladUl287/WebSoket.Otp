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
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        var connectionId = context.Connection.Id;
        var cancellationToken = context.RequestAborted;

        logger.HandshakeRequestStarted(connectionId);

        var requestValid = await ValidateRequestAsync(context, connectionId, cancellationToken);
        if (!requestValid)
        {
            return;
        }

        var handshakeRequest = await DeserializeHandshakeRequestAsync(context, connectionId, cancellationToken);
        if (handshakeRequest is null)
        {
            return;
        }

        var protocol = await ValidateProtocolAsync(context, handshakeRequest.Protocol, connectionId, cancellationToken);
        if (protocol is null)
        {
            return;
        }

        var authResult = await authService.AuhtorizeAsync(context, options.Authorization);
        if (authResult.Failed)
        {
            await WriteErrorResponseAsync(context, StatusCodes.Status401Unauthorized, authResult.FailureReason, cancellationToken);
            logger.WebSocketRequestAuthFailed(connectionId);
            return;
        }

        var connectionTokenId = await CreateConnectionAsync(context, options, handshakeRequest.Protocol, cancellationToken);

        await WriteSuccessResponseAsync(context, connectionTokenId, cancellationToken);

        //logger.HandshakeCompleted(connectionId, connectionTokenId);
    }

    private static async Task<bool> ValidateRequestAsync(HttpContext context, string connectionId, CancellationToken cancellationToken)
    {
        var request = context.Request;
        if (!HttpMethods.IsPost(request.Method))
        {
            const string errorMessage = "Method not allowed";
            //logger.RequestValidationFailed(connectionId, "Method", context.Request.Method);
            await WriteErrorResponseAsync(context, StatusCodes.Status405MethodNotAllowed, errorMessage, cancellationToken);
            return false;
        }

        const string JSON_CONTENT_TYPE = "application/json";
        if (request.ContentType.Contains(JSON_CONTENT_TYPE, StringComparison.OrdinalIgnoreCase) is false)
        {
            const string errorMessage = "Invalid content type";
            //logger.RequestValidationFailed(connectionId, "ContentType", context.Request.ContentType ?? "null");
            await WriteErrorResponseAsync(context, StatusCodes.Status415UnsupportedMediaType, errorMessage, cancellationToken);
            return false;
        }

        return true;
    }

    private async Task<HandshakeRequest?> DeserializeHandshakeRequestAsync(
        HttpContext context, string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            var handshakeRequest = await JsonSerializer.DeserializeAsync<HandshakeRequest>(
                context.Request.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                },
                cancellationToken);

            if (handshakeRequest is null)
            {
                //logger.HandshakeRequestDeserializationFailed(connectionId, "Null request body");
                await WriteErrorResponseAsync(context, StatusCodes.Status400BadRequest, "Invalid request body", cancellationToken);
            }

            return handshakeRequest;
        }
        catch (JsonException ex)
        {
            //logger.HandshakeRequestDeserializationFailed(connectionId, ex.Message);
            await WriteErrorResponseAsync(context, StatusCodes.Status400BadRequest, "Invalid JSON format", cancellationToken);
            return null;
        }
    }

    private async Task<ISerializer?> ValidateProtocolAsync(
        HttpContext context, string protocolName, string connectionId, CancellationToken cancellationToken)
    {
        var serializerFactory = context.RequestServices.GetRequiredService<ISerializerFactory>();
        var protocol = serializerFactory.Create(protocolName);

        if (protocol is null)
        {
            //logger.ProtocolValidationFailed(connectionId, protocolName);
            await WriteErrorResponseAsync(context, StatusCodes.Status400BadRequest, $"Protocol '{protocolName}' not supported", cancellationToken);
        }

        return protocol;
    }

    private async Task<string> CreateConnectionAsync(HttpContext context, WsMiddlewareOptions options, string protocolName, CancellationToken cancellationToken)
    {
        options.Connection.User = context.User;
        options.Connection.Protocol = WsProtocol.New(protocolName);

        var connectionTokenId = await requestState.GenerateTokenId(context, options.Connection, cancellationToken);

        logger.ConnectionTokenGenerated(connectionTokenId, context.Connection.Id);

        return connectionTokenId;
    }

    private static async Task WriteSuccessResponseAsync(
        HttpContext context, string responseContent, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync(responseContent, cancellationToken);
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string message, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain; charset=utf-8";

        if (!string.IsNullOrEmpty(message))
        {
            await context.Response.WriteAsync(message, cancellationToken);
        }
    }
}
