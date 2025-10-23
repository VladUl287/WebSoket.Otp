using Microsoft.Extensions.Logging;

namespace WebSockets.Otp.AspNet.Logging;

internal static partial class SequentialMessageProcessorLogging
{

    [LoggerMessage(Level = LogLevel.Warning, Message = "WebSocket authorization failed for {RemoteIp}. Reason: {Reason}")]
    internal static partial void LogAuthorizationFailed(this ILogger logger, string remoteIp, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting sequential message processing for connection {ConnectionId}")]
    internal static partial void LogStartingMessageProcessing(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Received close message for connection {ConnectionId}")]
    internal static partial void LogCloseMessageReceived(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message too big for connection {ConnectionId}. Current buffer: {BufferSize}, incoming chunk: {ChunkSize}, max size: {MaxMessageSize}")]
    internal static partial void LogMessageTooBig(this ILogger logger, string connectionId, int bufferSize, int chunkSize, int maxMessageSize);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing complete message for connection {ConnectionId}. Total size: {MessageSize}")]
    internal static partial void LogProcessingCompleteMessage(this ILogger logger, string connectionId, int messageSize);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Received message chunk for connection {ConnectionId}. Chunk size: {ChunkSize}, total buffer: {BufferSize}, endOfMessage: {EndOfMessage}")]
    internal static partial void LogMessageChunkReceived(this ILogger logger, string connectionId, int chunkSize, int bufferSize, bool endOfMessage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Message processing completed for connection {ConnectionId}. Reason: {Reason}")]
    internal static partial void LogMessageProcessingCompleted(this ILogger logger, string connectionId, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error during message processing for connection {ConnectionId}")]
    internal static partial void LogMessageProcessingError(this ILogger logger, Exception ex, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Buffer reclaimed for connection {ConnectionId}. Previous size: {PreviousSize}, new size: {NewSize}")]
    internal static partial void LogBufferReclaimed(this ILogger logger, string connectionId, int previousSize, int newSize);
}
