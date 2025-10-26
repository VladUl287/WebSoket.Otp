using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Core.Helpers;

namespace WebSockets.Otp.Core.Processors;

public sealed class ParallelMessageProcessor(
    IMessageDispatcher dispatcher, IMessageBufferFactory bufferFactory, ISerializerFactory serializerFactory,
    ILogger<SequentialMessageProcessor> logger) : IMessageProcessor
{
    public string Name => ProcessingMode.Parallel;

    public async Task Process(IWsConnection connection, WsMiddlewareOptions options)
    {
        var memoryOptions = options.Memory;

        var pool = new AsyncObjectPool<int, IMessageBuffer>(memoryOptions.MaxBufferPoolSize, bufferFactory.Create);
        var tempBuffer = ArrayPool<byte>.Shared.Rent(memoryOptions.InitialBufferSize);

        var reclaimBuffer = memoryOptions.ReclaimBuffersImmediately;
        var serializer = serializerFactory.Resolve(options.Connection.Protocol);
        try
        {
            var enumerable = EnumerateMessages(connection, options, pool, tempBuffer);
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = options.Processing.MaxParallelOperations,
                CancellationToken = connection.Context.RequestAborted
            };
            await Parallel.ForEachAsync(enumerable, parallelOptions, async (buffer, token) =>
            {
                using var manager = buffer.Manager;
                await dispatcher.DispatchMessage(connection, serializer, manager.Memory, token);

                buffer.SetLength(0);

                if (reclaimBuffer)
                    buffer.Shrink();

                await pool.Return(buffer);
            });
        }
        finally
        {
            await pool.DisposeAsync();
            ArrayPool<byte>.Shared.Return(tempBuffer);
        }
    }

    private async IAsyncEnumerable<IMessageBuffer> EnumerateMessages(
        IWsConnection connection, WsMiddlewareOptions options, AsyncObjectPool<int, IMessageBuffer> pool, byte[] tempBuffer)
    {
        var connectionId = connection.Id;

        var maxMessageSize = options.Memory.MaxMessageSize;
        var bufferSize = options.Memory.InitialBufferSize;

        var socket = connection.Socket;
        var token = connection.Context.RequestAborted;

        IMessageBuffer? buffer = null;
        while (socket.State is WebSocketState.Open && !token.IsCancellationRequested)
        {
            var wsMessage = await socket.ReceiveAsync(tempBuffer, token);

            if (wsMessage.MessageType is WebSocketMessageType.Close)
            {
                logger.LogCloseMessageReceived(connectionId);
                await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                break;
            }

            buffer ??= await pool.Rent(bufferSize);

            if (buffer.Length > maxMessageSize - wsMessage.Count)
            {
                logger.LogMessageTooBig(connectionId, buffer.Length, wsMessage.Count, maxMessageSize);
                await connection.Socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message exceeds size limit", CancellationToken.None);
                break;
            }

            buffer.Write(tempBuffer.AsSpan(0, wsMessage.Count));

            if (wsMessage.EndOfMessage)
            {
                yield return buffer;
                buffer = null;
            }
        }
    }
}
