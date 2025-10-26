using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.Core.Logging;

internal static partial class WsAuthorizationServiceLogging
{
    [LoggerMessage(LogLevel.Debug, "Authorization not required.")]
    internal static partial void LogAuthorizationNotRequired(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Authorization service is missing.")]
    internal static partial void LogAuthorizationServiceMissing(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "User is not authenticated.")]
    internal static partial void LogUserNotAuthenticated(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Authorization failed for policy {Policy}: {FailureReason}")]
    internal static partial void LogPolicyAuthorizationFailed(this ILogger logger, string policy, string failureReason);

    [LoggerMessage(LogLevel.Warning, "User is not in the required role.")]
    internal static partial void LogUserNotInRequiredRole(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "User authorization scheme is not valid.")]
    internal static partial void LogInvalidAuthorizationScheme(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Authorization succeeded.")]
    internal static partial void LogAuthorizationSucceeded(this ILogger logger);
}
