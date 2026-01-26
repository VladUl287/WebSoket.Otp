using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.Core.Logging;

internal static partial class MessageProcessorLogging
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to dispatch message")]
    internal static partial void LogDispatchMessageFailure(this ILogger logger, Exception exception);
}
