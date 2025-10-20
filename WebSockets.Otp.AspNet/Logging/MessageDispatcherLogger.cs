using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.AspNet.Logging;

internal static partial class MessageDispatcherLogger
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Dispatching message for connection {ConnectionId}. Endpoint key: {EndpointKey}, Payload size: {PayloadSize}")]
    internal static partial void LogDispatchingMessage(this ILogger logger, string connectionId, string endpointKey, int payloadSize);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to extract endpoint key from message for connection {ConnectionId}")]
    internal static partial void LogKeyExtractionFailed(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Endpoint not found in registry for connection {ConnectionId}. Endpoint key: {EndpointKey}")]
    internal static partial void LogEndpointNotFound(this ILogger logger, string connectionId, string endpointKey);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Endpoint service not found in DI container for connection {ConnectionId}. Endpoint type: {EndpointType}")]
    internal static partial void LogEndpointServiceNotFound(this ILogger logger, string connectionId, string endpointType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Successfully resolved endpoint for connection {ConnectionId}. Endpoint type: {EndpointType}, Has request: {HasRequest}")]
    internal static partial void LogEndpointResolved(this ILogger logger, string connectionId, string endpointType, bool hasRequest);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully dispatched message for connection {ConnectionId}. Endpoint key: {EndpointKey}")]
    internal static partial void LogMessageDispatched(this ILogger logger, string connectionId, string endpointKey);
}
