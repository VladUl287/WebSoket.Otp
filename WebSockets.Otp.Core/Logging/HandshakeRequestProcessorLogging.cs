using Microsoft.Extensions.Logging;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Logging;

internal static partial class HandshakeRequestProcessorLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Handshake request started. TraceId: {TraceId}")]
    internal static partial void HandshakeRequestStarted(this ILogger logger, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse handshake request. TraceId: {TraceId}")]
    internal static partial void HandshakeBodyParseFail(this ILogger logger, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported protocol requested. TraceId: {TraceId}, Protocol: {Protocol}")]
    internal static partial void HandshakedUnsupportedProtocol(this ILogger logger, TraceId traceId, string protocol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handshake completed successfully: {TraceId}")]
    internal static partial void HandshakeRequestCompleted(this ILogger logger, TraceId traceId);
}
