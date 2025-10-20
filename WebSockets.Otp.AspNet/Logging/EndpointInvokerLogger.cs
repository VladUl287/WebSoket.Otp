using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.AspNet.Logging;

public static partial class EndpointInvokerLogger
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Invoking endpoint {EndpointType} for connection {ConnectionId}")]
    public static partial void LogInvokingEndpoint(this ILogger logger, string endpointType, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Creating invoker for endpoint type {EndpointType}")]
    public static partial void LogCreatingInvoker(this ILogger logger, string endpointType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache miss for endpoint type {EndpointType}, creating new invoker")]
    public static partial void LogCacheMiss(this ILogger logger, string endpointType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for endpoint type {EndpointType}")]
    public static partial void LogCacheHit(this ILogger logger, string endpointType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully invoked endpoint {EndpointType} for connection {ConnectionId}")]
    public static partial void LogInvocationSuccess(this ILogger logger, string endpointType, string connectionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to invoke endpoint {EndpointType} for connection {ConnectionId}")]
    public static partial void LogInvocationFailed(this ILogger logger, Exception ex, string endpointType, string connectionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to resolve handle method for endpoint type {EndpointType}")]
    public static partial void LogMethodResolutionFailed(this ILogger logger, string endpointType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Method resolution returned null for endpoint type {EndpointType}")]
    public static partial void LogNullMethodResolution(this ILogger logger, string endpointType);
}
