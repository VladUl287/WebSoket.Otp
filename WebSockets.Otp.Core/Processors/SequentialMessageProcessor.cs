using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Logging;

namespace WebSockets.Otp.Core.Processors;

public sealed class SequentialMessageProcessor(
    IMessageDispatcher dispatcher, IMessageBufferFactory bufferFactory, ISerializerFactory serializerFactory,
    ILogger<SequentialMessageProcessor> logger) : IMessageProcessor
{
    public string Name => ProcessingMode.Sequential;

    public async Task Process(IWsConnection connection, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);

        var buffer = bufferFactory.Create(options.Memory.InitialBufferSize);
        var tempBuffer = ArrayPool<byte>.Shared.Rent(options.Memory.InitialBufferSize);
        try
        {
            await SequentialMessageProcessoLoop(connection, options, buffer, tempBuffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
            buffer.Dispose();
        }
    }

    private async Task SequentialMessageProcessoLoop(IWsConnection connection, WsMiddlewareOptions options, IMessageBuffer buffer, byte[] tempBuffer)
    {
        var connectionId = connection.Id;

        var maxMessageSize = options.Memory.MaxMessageSize;
        var reclaimBuffer = options.Memory.ReclaimBuffersImmediately;

        var socket = connection.Socket;
        var token = connection.Context.RequestAborted;

        var serializer = serializerFactory.Create(options.Connection.Protocol);
        try
        {
            while (socket.State is WebSocketState.Open && !token.IsCancellationRequested)
            {
                var wsMessage = await socket.ReceiveAsync(tempBuffer, token);
                logger.LogMessageChunkReceived(connectionId, wsMessage.Count, buffer.Length, wsMessage.EndOfMessage);

                if (wsMessage.MessageType is WebSocketMessageType.Close)
                {
                    logger.LogCloseMessageReceived(connectionId);
                    await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                if (buffer.Length > maxMessageSize - wsMessage.Count)
                {
                    logger.LogMessageTooBig(connectionId, buffer.Length, wsMessage.Count, maxMessageSize);
                    await connection.Socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message exceeds size limit", CancellationToken.None);
                    break;
                }

                buffer.Write(tempBuffer.AsSpan(0, wsMessage.Count));

                if (wsMessage.EndOfMessage)
                {
                    logger.LogProcessingCompleteMessage(connectionId, buffer.Length);

                    using var manager = buffer.Manager;
                    await dispatcher.DispatchMessage(connection, serializer, manager.Memory, token);

                    buffer.SetLength(0);

                    if (reclaimBuffer)
                    {
                        var previousSize = buffer.Capacity;
                        buffer.Shrink();
                        logger.LogBufferReclaimed(connectionId, previousSize, buffer.Capacity);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            logger.LogMessageProcessingCompleted(connectionId, "Request cancelled");
        }
        catch (Exception ex)
        {
            logger.LogMessageProcessingError(ex, connectionId);
            throw;
        }
    }
}
