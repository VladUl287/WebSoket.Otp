using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.Core.Logging;

internal static partial class DefaultMessageDispatcherLogging
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Authorization failed. Reason: '{Reason}'")]
    internal static partial void AuthorizationFailed(this ILogger logger, string Reason);
    
    [LoggerMessage(Level = LogLevel.Error, Message = "Message key field is required")]
    internal static partial void KeyFieldMissing(this ILogger logger);
}
