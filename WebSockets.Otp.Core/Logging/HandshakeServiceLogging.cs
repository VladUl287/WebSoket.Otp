using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.Core.Logging;

internal static partial class HandshakeServiceLogging
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting handshake process. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeProcessStarted(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "No message reader found for protocol '{Protocol}' during handshake. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeMessageReaderNotFound(this ILogger logger, string protocol, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Message reader obtained for protocol '{Protocol}'. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeMessageReaderObtained(this ILogger logger, string protocol, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting await handshake message. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeAwaitHandshakeMessage(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to read handshake message. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeFailReadHandshakeMessage(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handshake message received ({Length} bytes). ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeMessageReceived(this ILogger logger, int length, string connectionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "No serializer found for protocol '{Protocol}' during handshake. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeSerializerNotFound(this ILogger logger, string protocol, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Serializer obtained for protocol '{Protocol}'. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeSerializerObtained(this ILogger logger, string protocol, string connectionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize handshake options. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeDeserializeFailed(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Start sending handshake response. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeStartResponseSending(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Finishing handshake process. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeProcessFinished(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Buffer returned to pool. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeBufferReturnedToPool(this ILogger logger, string connectionId);
}
