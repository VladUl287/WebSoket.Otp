using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Helpers;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Processors;

public sealed class ParallelMessageProcessor(
    IMessageDispatcher dispatcher, IMessageBufferFactory bufferFactory, ISerializerFactory serializerFactory,
    ILogger<SequentialMessageProcessor> logger) : IMessageProcessor
{
    public string Name => ProcessingMode.Parallel;

    public async Task Process(IWsConnection connection, WsMiddlewareOptions options)
    {
        var memoryOptions = options.Memory;
        var processingOptions = options.Processing;
        var cancellationToken = connection.Context.RequestAborted;

        var bufferPool = new AsyncObjectPool<int, IMessageBuffer>(memoryOptions.MaxBufferPoolSize, bufferFactory.Create);
        var tempBuffer = ArrayPool<byte>.Shared.Rent(memoryOptions.InitialBufferSize);

        try
        {
            var serializer = serializerFactory.Resolve(options.Connection.Protocol);
            var enumerable = EnumerateMessages(connection, options, bufferPool, tempBuffer);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = processingOptions.MaxParallelOperations,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(enumerable, parallelOptions, async (buffer, token) =>
            {
                try
                {
                    await dispatcher.DispatchMessage(connection, serializer, buffer, token);
                }
                finally
                {
                    buffer.SetLength(0);
                    if (memoryOptions.ReclaimBuffersImmediately)
                        buffer.Shrink();

                    await bufferPool.Return(buffer);
                }
            });
        }
        finally
        {
            await bufferPool.DisposeAsync();
            ArrayPool<byte>.Shared.Return(tempBuffer);
        }
    }

    private async IAsyncEnumerable<IMessageBuffer> EnumerateMessages(
        IWsConnection connection, WsMiddlewareOptions options, AsyncObjectPool<int, IMessageBuffer> pool, byte[] tempBuffer)
    {
        var connectionId = connection.Id;
        var socket = connection.Socket;
        var token = connection.Context.RequestAborted;
        var maxMessageSize = options.Memory.MaxMessageSize;
        var bufferSize = options.Memory.InitialBufferSize;

        IMessageBuffer? currentBuffer = null;
        while (socket.State is WebSocketState.Open && !token.IsCancellationRequested)
        {
            var wsMessage = await socket.ReceiveAsync(tempBuffer, token);

            if (wsMessage.MessageType is WebSocketMessageType.Close)
            {
                logger.LogCloseMessageReceived(connectionId);
                await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                break;
            }

            currentBuffer ??= await pool.Rent(bufferSize);

            if (currentBuffer.Length > maxMessageSize - wsMessage.Count)
            {
                logger.LogMessageTooBig(connectionId, currentBuffer.Length, wsMessage.Count, maxMessageSize);
                await connection.Socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message exceeds size limit", CancellationToken.None);
                break;
            }

            currentBuffer.Write(tempBuffer.AsSpan(0, wsMessage.Count));

            if (wsMessage.EndOfMessage)
            {
                yield return currentBuffer;
                currentBuffer = null;
            }
        }
    }
}
