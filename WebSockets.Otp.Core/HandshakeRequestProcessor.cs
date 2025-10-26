using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Logging;

using OutFailure = (int Code, string Reason);

namespace WebSockets.Otp.Core;

public sealed class HandshakeRequestProcessor(
    IWsAuthorizationService authService,
    IConnectionStateService requestState,
    ISerializerFactory serializerFactory,
    ILogger<HandshakeRequestProcessor> logger) : IHandshakeRequestProcessor
{
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

        var requestValid = ValidateRequest(context, options, out var failureReason);
        if (!requestValid)
        {
            var (code, reason) = failureReason.Value;
            await WriteErrorResponseAsync(context, code, reason, cancellationToken);
            return;
        }

        var handshakeRequest = await DeserializeHandshakeRequestAsync(context, cancellationToken);
        if (handshakeRequest is null)
        {
            return;
        }

        var protocolValid = ValidateProtocolAsync(handshakeRequest.Protocol, out var protocolFailure);
        if (!protocolValid)
        {
            var (code, reason) = protocolFailure.Value;
            await WriteErrorResponseAsync(context, code, reason, cancellationToken);
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

        logger.HandshakeCompleted(connectionId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsHandshakeRequestPath(HttpContext context, WsMiddlewareOptions options) => context.Request.Path.Equals(options.Paths.HandshakePath);

    private static bool ValidateRequest(HttpContext context, WsMiddlewareOptions options, [NotNullWhen(false)] out OutFailure? failureReason)
    {
        failureReason = null;

        if (!IsHandshakeRequestPath(context, options))
        {
            const string errorMessage = "Not hahdshake request path";
            //logger.RequestValidationFailed(connectionId, "Method", context.Request.Method);
            failureReason = (StatusCodes.Status403Forbidden, errorMessage);
            return false;
        }

        var request = context.Request;
        if (!HttpMethods.IsPost(request.Method))
        {
            const string errorMessage = "Method not allowed";
            //logger.RequestValidationFailed(connectionId, "Method", context.Request.Method);
            failureReason = (StatusCodes.Status405MethodNotAllowed, errorMessage);
            return false;
        }

        const string JSON_CONTENT_TYPE = "application/json";
        if (request.ContentType.Contains(JSON_CONTENT_TYPE, StringComparison.OrdinalIgnoreCase) is false)
        {
            const string errorMessage = "Invalid content type. Only supported application/json";
            //logger.RequestValidationFailed(connectionId, "ContentType", context.Request.ContentType ?? "null");
            failureReason = (StatusCodes.Status415UnsupportedMediaType, errorMessage);
            return false;
        }

        return true;
    }

    private static readonly JsonSerializerOptions _opitons = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<ConnectionSettings?> DeserializeHandshakeRequestAsync(HttpContext context, CancellationToken token)
    {
        try
        {
            var handshakeRequest = await JsonSerializer.DeserializeAsync<ConnectionSettings>(
                context.Request.Body,
                _opitons,
                token);

            if (handshakeRequest is null)
            {
                //logger.HandshakeRequestDeserializationFailed(connectionId, "Null request body");
                await WriteErrorResponseAsync(context, StatusCodes.Status400BadRequest, "Invalid request body", token);
            }

            return handshakeRequest;
        }
        catch (Exception ex)
        {
            //logger.HandshakeRequestDeserializationFailed(connectionId, ex.Message);
            await WriteErrorResponseAsync(context, StatusCodes.Status400BadRequest, "Invalid JSON format", token);
            return null;
        }
    }

    private bool ValidateProtocolAsync(string protocolName, [NotNullWhen(false)] out OutFailure? failure)
    {
        failure = null;

        var protocol = serializerFactory.Create(protocolName);
        if (protocol is null)
        {
            //logger.ProtocolValidationFailed(connectionId, protocolName);
            failure = (StatusCodes.Status400BadRequest, $"Protocol '{protocolName}' not supported");
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
