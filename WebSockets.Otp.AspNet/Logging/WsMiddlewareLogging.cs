using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.AspNet.Logging;

internal static partial class WsMiddlewareLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket handshake request started for {ConnectionId}")]
    internal static partial void HandshakeRequestStarted(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket request matched")]
    internal static partial void WebSocketRequestMatched(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Authorization failed: {FailureReason}")]
    internal static partial void AuthorizationFailed(this ILogger logger, string failureReason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing WsMiddleware request")]
    internal static partial void WsMiddlewareError(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection token generated: {ConnectionToken}. Session id: {ConnectionId}")]
    internal static partial void ConnectionTokenGenerated(this ILogger logger, string connectionToken, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "User authenticated: {UserName}")]
    internal static partial void UserAuthenticated(this ILogger logger, string userName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Missing connection token: {ConnectionId}")]
    internal static partial void MissingConnectionToken(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid connection token: {ConnectionTokenId}")]
    internal static partial void InvalidConnectionToken(this ILogger logger, string connectionTokenId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Authorization failed for connection: {ConnectionId}, User: {UserName}")]
    internal static partial void AuthorizationFailed(this ILogger logger, string connectionId, string userName);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebSocket connection established: {ConnectionId}, User: {UserName}")]
    internal static partial void WebSocketConnectionEstablished(this ILogger logger, string connectionId, string userName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing WebSocket request: {ConnectionId}")]
    internal static partial void WebSocketRequestError(this ILogger logger, string connectionId, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection token resolved: {ConnectionTokenId}")]
    internal static partial void ConnectionTokenResolved(this ILogger logger, string connectionTokenId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "User context set from connection options: {UserName}")]
    internal static partial void UserContextSet(this ILogger logger, string userName);
}
