using Microsoft.Extensions.Logging;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Logging;

internal static partial class HandshakeRequestProcessorLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket handshake request started for {TraceId}")]
    internal static partial void HandshakeRequestStarted(this ILogger logger, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handshake completed successfully: {TraceId}")]
    internal static partial void HandshakeRequestCompleted(this ILogger logger, TraceId traceId);
}
