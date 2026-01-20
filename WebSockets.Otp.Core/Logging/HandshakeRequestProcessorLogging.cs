using Microsoft.Extensions.Logging;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Logging;

internal static partial class HandshakeRequestProcessorLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Handshake request started. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeRequestStarted(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse handshake request. ConnectionId: {ConnectionId}")]
    internal static partial void HandshakeBodyParseFail(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported protocol requested. ConnectionId: {ConnectionId}, Protocol: {Protocol}")]
    internal static partial void HandshakedUnsupportedProtocol(this ILogger logger, string connectionId, string protocol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handshake completed successfully: {ConnectionId}")]
    internal static partial void HandshakeRequestCompleted(this ILogger logger, string connectionId);
}
