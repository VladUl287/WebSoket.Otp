using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.Core.Logging;

internal static partial class DefaultMessageDispatcherLogging
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Request was rejected with HTTP status {StatusCode}")]
    internal static partial void AuthFailed(this ILogger logger, int StatusCode);
    
    [LoggerMessage(Level = LogLevel.Error, Message = "Message key field is required")]
    internal static partial void MessageKeyFieldMissing(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Serializer returned an invalid endpoint key index: {KeyIndex}. Payload length: {PayloadLength}")]
    internal static partial void FailToResolveFieldInfo(this ILogger logger, int KeyIndex, int PayloadLength);
}
