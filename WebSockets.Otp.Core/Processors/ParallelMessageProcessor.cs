using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Logging;
using WebSockets.Otp.Core.Helpers;

namespace WebSockets.Otp.Core.Processors;

public sealed class ParallelMessageProcessor(
    IMessageDispatcher dispatcher, IMessageBufferFactory bufferFactory, ISerializerFactory serializerFactory,
    ILogger<SequentialMessageProcessor> logger) : IMessageProcessor
{
    public string Name => ProcessingMode.Parallel;

    public async Task Process(IWsConnection connection, WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(connection, nameof(connection));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var pool = new AsyncObjectPool<IMessageBuffer>(options.Memory.MaxBufferPoolSize, () => bufferFactory.Create(options.Memory.InitialBufferSize));
        var tempBuffer = ArrayPool<byte>.Shared.Rent(options.Memory.InitialBufferSize);

        var reclaimBuffer = options.Memory.ReclaimBuffersImmediately;

        var serializer = serializerFactory.Create(options.Connection.Protocol);
        try
        {
            var enumerable = EnumerateMessagesAsync(connection, options, pool, tempBuffer);
            await Parallel.ForEachAsync(enumerable, new ParallelOptions
            {
                MaxDegreeOfParallelism = options.Processing.MaxParallelOperations,
                CancellationToken = connection.Context.RequestAborted
            }, async (buffer, token) =>
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
            ArrayPool<byte>.Shared.Return(tempBuffer);
            await pool.DisposeAsync();
        }
    }

    private async IAsyncEnumerable<IMessageBuffer> EnumerateMessagesAsync(
        IWsConnection connection, WsMiddlewareOptions options, AsyncObjectPool<IMessageBuffer> pool, byte[] tempBuffer)
    {
        var connectionId = connection.Id;

        var maxMessageSize = options.Memory.MaxMessageSize;

        var socket = connection.Socket;
        var token = connection.Context.RequestAborted;

        IMessageBuffer? buffer = null;
        while (socket.State is WebSocketState.Open && !token.IsCancellationRequested)
        {
            var wsMessage = await socket.ReceiveAsync(tempBuffer, token);
            if (wsMessage.MessageType is WebSocketMessageType.Close)
            {
                await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                break;
            }

            if (wsMessage.MessageType is WebSocketMessageType.Close)
            {
                logger.LogCloseMessageReceived(connectionId);
                await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                break;
            }

            buffer ??= await pool.Rent();

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
