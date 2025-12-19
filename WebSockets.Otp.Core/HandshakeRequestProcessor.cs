using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core;

public sealed class HandshakeRequestProcessor(
    IWsAuthorizationService authService,
    IConnectionStateService requestState,
    ISerializerFactory serializerFactory,
    ILogger<HandshakeRequestProcessor> logger) : IHandshakeRequestProcessor
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string JsonContentType = "application/json";
    private const string TextContentType = "text/plain; charset=utf-8";

    public bool IsHandshakeRequest(HttpContext context, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        return IsHandshakeRequestPath(context, options);
    }

    public async Task HandleRequestAsync(HttpContext context, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        var connectionId = context.Connection.Id;
        var cancellationToken = context.RequestAborted;

        logger.HandshakeRequestStarted(connectionId);

        if (!ValidateRequest(context, options, out var statusCode, out var errorMessage))
        {
            await WriteResponseAsync(context, statusCode, errorMessage, cancellationToken);
            return;
        }

        var handshakeRequest = await DeserializeHandshakeRequestAsync(context, cancellationToken);
        if (handshakeRequest is null)
        {
            return;
        }

        if (!ValidateConnection(handshakeRequest.Protocol, out statusCode, out errorMessage))
        {
            await WriteResponseAsync(context, statusCode, errorMessage, cancellationToken);
            return;
        }

        var authorized = await authService.TryAuhtorize(context, options.Authorization);
        if (!authorized)
        {
            await WriteResponseAsync(context, StatusCodes.Status401Unauthorized, string.Empty, cancellationToken);
            logger.WebSocketRequestAuthFailed(connectionId);
            return;
        }

        var connectionTokenId = await CreateConnectionAsync(context, options, handshakeRequest.Protocol, cancellationToken);
        await WriteResponseAsync(context, StatusCodes.Status200OK, connectionTokenId, cancellationToken);

        logger.HandshakeCompleted(connectionId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsHandshakeRequestPath(HttpContext context, WsMiddlewareOptions options) => context.Request.Path.Equals(options.Paths.HandshakePath);

    private static bool ValidateRequest(HttpContext context, WsMiddlewareOptions options, out int statusCode, [NotNullWhen(false)] out string? errorMessage)
    {
        statusCode = 0;
        errorMessage = null;

        if (!IsHandshakeRequestPath(context, options))
        {
            statusCode = StatusCodes.Status403Forbidden;
            errorMessage = "Not handshake request path";
            return false;
        }

        var request = context.Request;
        if (!HttpMethods.IsPost(request.Method))
        {
            statusCode = StatusCodes.Status405MethodNotAllowed;
            errorMessage = "Method not allowed";
            return false;
        }

        if (!request.ContentType?.Contains(JsonContentType, StringComparison.OrdinalIgnoreCase) ?? true)
        {
            statusCode = StatusCodes.Status415UnsupportedMediaType;
            errorMessage = "Invalid content type. Only supported application/json";
            return false;
        }

        return true;
    }

    private async Task<ConnectionSettings?> DeserializeHandshakeRequestAsync(HttpContext context, CancellationToken token)
    {
        try
        {
            var handshakeRequest = await JsonSerializer.DeserializeAsync<ConnectionSettings>(
                context.Request.Body,
                _jsonOptions,
                token);

            if (handshakeRequest is null)
            {
                await WriteResponseAsync(context, StatusCodes.Status400BadRequest, "Invalid request body", token);
                return null;
            }

            return handshakeRequest;
        }
        catch (Exception ex)
        {
            logger.HandshakeRequestDeserializationFailed(context.Connection.Id, ex);
            await WriteResponseAsync(context, StatusCodes.Status400BadRequest, "Invalid JSON format", token);
            return null;
        }
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

    private static Task WriteResponseAsync(HttpContext context, int statusCode, string message, CancellationToken token)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = TextContentType;
        return context.Response.WriteAsync(message, token);
    }
}
