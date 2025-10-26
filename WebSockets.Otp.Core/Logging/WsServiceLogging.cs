using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.Core.Logging;

internal static partial class WsServiceLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to add connection {ConnectionId} to connection manager")]
    internal static partial void LogFailedToAddConnection(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket connection established: {ConnectionId}")]
    internal static partial void LogConnectionEstablished(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket connection closed: {ConnectionId}")]
    internal static partial void LogConnectionClosed(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error executing {OperationName} handler")]
    internal static partial void LogHandlerFail(this ILogger logger, string operationName, Exception ex);
}