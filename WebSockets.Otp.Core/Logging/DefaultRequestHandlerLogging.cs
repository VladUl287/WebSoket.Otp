using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Enums;

namespace WebSockets.Otp.Core.Logging;

internal static partial class DefaultRequestHandlerLogging
{
    [LoggerMessage(
    Level = LogLevel.Error, Message = "HTTP context not found in connection context")]
    public static partial void HttpContextNotFound(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "WebSocket request processing started")]
    public static partial void RequestProcessingStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Handshake options not found")]
    public static partial void HandshakeOptionsNotFound(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handshake completed. Protocol: {protocol}")]
    public static partial void HandshakeCompleted(this ILogger logger, string protocol);

    [LoggerMessage(Level = LogLevel.Error, Message = "Message reader not found for protocol: {protocol}")]
    public static partial void MessageReaderNotFound(this ILogger logger, string protocol);

    [LoggerMessage(Level = LogLevel.Error, Message = "Serializer not found for protocol: {protocol}")]
    public static partial void SerializerNotFound(this ILogger logger, string protocol);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to add connection: {connectionId}")]
    public static partial void ConnectionAddFailed(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection established: {connectionId}")]
    public static partial void ConnectionEstablished(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Invoking OnConnected callback for connection: {connectionId}")]
    public static partial void InvokingOnConnectedCallback(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting message processing for connection: {connectionId}, mode: {processingMode}")]
    public static partial void MessageProcessingStarted(this ILogger logger, string connectionId, ProcessingMode processingMode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Message processing completed for connection: {connectionId}")]
    public static partial void MessageProcessingCompleted(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Removing connection: {connectionId}")]
    public static partial void RemovingConnection(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Invoking OnDisconnected callback for connection: {connectionId}")]
    public static partial void InvokingOnDisconnectedCallback(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection closed: {connectionId}")]
    public static partial void ConnectionClosed(this ILogger logger, string connectionId);
}
