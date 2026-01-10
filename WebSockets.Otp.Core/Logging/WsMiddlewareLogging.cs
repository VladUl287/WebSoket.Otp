using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.Core.Logging;

internal static partial class WsMiddlewareLogging
{

    [LoggerMessage(Level = LogLevel.Warning, Message = "WebSocket request authorization failed for {ConnectionId}")]
    internal static partial void WebSocketRequestAuthFailed(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket request matched")]
    internal static partial void WebSocketRequestMatched(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Authorization failed: {FailureReason}")]
    internal static partial void AuthorizationFailed(this ILogger logger, string failureReason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing WsMiddleware request")]
    internal static partial void WsMiddlewareError(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection token generated: {ConnectionToken}. Session id: {ConnectionId}")]
    internal static partial void ConnectionTokenGenerated(this ILogger logger, string connectionToken, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid connection token: {ConnectionTokenId}")]
    internal static partial void InvalidConnectionToken(this ILogger logger, string connectionTokenId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Authorization failed for connection: {ConnectionId}, User: {UserName}")]
    internal static partial void AuthorizationFailed(this ILogger logger, string connectionId, string userName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "User context set from connection options: {UserName}")]
    internal static partial void UserContextSet(this ILogger logger, string userName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Handshake authorization failed: {ConnectionId} - {Reason}")]
    internal static partial void HandshakeAuthorizationFailed(this ILogger logger, string connectionId, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handshake completed successfully: {ConnectionId}")]
    internal static partial void HandshakeCompleted(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Handshake request failed: {ConnectionId}")]
    internal static partial void HandshakeRequestFailed(this ILogger logger, string connectionId, Exception ex);
}
