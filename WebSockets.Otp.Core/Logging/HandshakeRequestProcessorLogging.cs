using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.Core.Logging;

internal static partial class HandshakeRequestProcessorLogging
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Handshake request deserialization failed for connection: {connectionId}")]
    internal static partial void HandshakeRequestDeserializationFailed(this ILogger logger, string connectionId, Exception exception);
}
