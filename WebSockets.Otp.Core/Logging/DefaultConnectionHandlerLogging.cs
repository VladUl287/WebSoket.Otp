using Microsoft.Extensions.Logging;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Logging;

internal static partial class DefaultConnectionHandlerLogging
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "WebSocket request processing started. TraceId - '{traceId}'")]
    public static partial void RequestProcessingStarted(this ILogger logger, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Handshake options not found. TraceId - '{traceId}'")]
    public static partial void HandshakeOptionsNotFound(this ILogger logger, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handshake completed. Protocol - '{protocol}'. TraceId - '{traceId}'")]
    public static partial void HandshakeCompleted(this ILogger logger, string protocol, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Serializer not found for protocol. Protocol - '{protocol}'. TraceId - '{traceId}'")]
    public static partial void SerializerNotFound(this ILogger logger, string protocol, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to add connection. ConnectionId - '{ConnectionId}'. TraceId - '{traceId}'")]
    public static partial void ConnectionAddFailed(this ILogger logger, string connectionId, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection established. ConnectionId - '{ConnectionId}'. TraceId - '{traceId}'")]
    public static partial void ConnectionEstablished(this ILogger logger, string connectionId, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Invoking OnConnected callback. ConnectionId - '{ConnectionId}'. TraceId - '{traceId}'")]
    public static partial void InvokingOnConnectedCallback(this ILogger logger, string connectionId, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting message processing. TraceId - '{traceId}'")]
    public static partial void MessageProcessingStarted(this ILogger logger, string connectionId, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Message processing completed. ConnectionId - '{ConnectionId}'. TraceId - '{traceId}'")]
    public static partial void MessageProcessingCompleted(this ILogger logger, string connectionId, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Removing connection. ConnectionId - '{ConnectionId}'. TraceId - '{traceId}'")]
    public static partial void RemovingConnection(this ILogger logger, string connectionId, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Invoking OnDisconnected callback. ConnectionId - '{ConnectionId}'. TraceId - '{traceId}'")]
    public static partial void InvokingOnDisconnectedCallback(this ILogger logger, string connectionId, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection closed. ConnectionId - '{ConnectionId}'. TraceId - '{traceId}'")]
    public static partial void ConnectionClosed(this ILogger logger, string connectionId, TraceId traceId);
}
