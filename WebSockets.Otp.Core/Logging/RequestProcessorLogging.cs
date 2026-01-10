using Microsoft.Extensions.Logging;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Logging;

public static partial class RequestProcessorLogging
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "WebSocket request process started. TraceId: {TraceId}")]
    internal static partial void WsRequestProcessorStarted(this ILogger logger, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "WebSocket request process finished. TraceId: {TraceId}")]
    internal static partial void WsRequestProcessorFinished(this ILogger logger, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Missed connection token. TraceId: {TraceId}")]
    internal static partial void WsRequestMissedConnectionToken(this ILogger logger, TraceId traceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid connection token. TraceId: {TraceId}. Token: {ConnectionTokenId}")]
    internal static partial void WsRequestInvalidConnectionToken(this ILogger logger, TraceId traceId, string connectionTokenId);
}
