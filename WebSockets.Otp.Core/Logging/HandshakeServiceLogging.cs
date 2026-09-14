using Microsoft.Extensions.Logging;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Logging;

internal static partial class HandshakeServiceLogging
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting handshake process. TraceId: {TraceId}")]
    internal static partial void HandshakeProcessStarted(this ILogger logger, RequestId traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting await handshake message. TraceId: {TraceId}")]
    internal static partial void HandshakeAwaitHandshakeMessage(this ILogger logger, RequestId TraceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to read handshake message. TraceId: {TraceId}")]
    internal static partial void HandshakeFailReadHandshakeMessage(this ILogger logger, RequestId TraceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handshake message received ({Length} bytes). TraceId: {TraceId}")]
    internal static partial void HandshakeMessageReceived(this ILogger logger, int length, RequestId TraceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "No serializer found for protocol '{Protocol}' during handshake. TraceId: {TraceId}")]
    internal static partial void HandshakeSerializerNotFound(this ILogger logger, string protocol, RequestId TraceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Serializer obtained for protocol '{Protocol}'. TraceId: {TraceId}")]
    internal static partial void HandshakeSerializerObtained(this ILogger logger, string protocol, RequestId TraceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize handshake options. TraceId: {TraceId}")]
    internal static partial void HandshakeDeserializeFailed(this ILogger logger, RequestId TraceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Start sending handshake response. TraceId: {TraceId}")]
    internal static partial void HandshakeStartResponseSending(this ILogger logger, RequestId TraceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Finishing handshake process. TraceId: {TraceId}")]
    internal static partial void HandshakeProcessFinished(this ILogger logger, RequestId TraceId);
}
