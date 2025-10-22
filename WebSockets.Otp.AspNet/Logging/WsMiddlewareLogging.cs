using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.AspNet.Logging;

internal static partial class WsMiddlewareLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket handshake request matched")]
    internal static partial void HandshakeRequestMatched(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket request matched")]
    internal static partial void WebSocketRequestMatched(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Authorization failed: {FailureReason}")]
    internal static partial void AuthorizationFailed(this ILogger logger, string failureReason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing WsMiddleware request")]
    internal static partial void WsMiddlewareError(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection token generated: {ConnectionToken}")]
    internal static partial void ConnectionTokenGenerated(this ILogger logger, string connectionToken);

    [LoggerMessage(Level = LogLevel.Debug, Message = "User authenticated: {UserName}")]
    internal static partial void UserAuthenticated(this ILogger logger, string userName);
}
