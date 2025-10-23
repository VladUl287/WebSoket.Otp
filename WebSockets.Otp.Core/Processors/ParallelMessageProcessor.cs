using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Helpers;

namespace WebSockets.Otp.Core.Processors;

public sealed class ParallelMessageProcessor(
    IMessageDispatcher dispatcher, IMessageBufferFactory bufferFactory, ISerializerFactory serializerFactory,
    ILogger<SequentialMessageProcessor> logger) : IMessageProcessor
{
    public string Name => MessageProcessingModes.Parallel;

    public async Task Process(IWsConnection connection, WsMiddlewareOptions options)
    {
        var pool = new AsyncObjectPool<IMessageBuffer>(10, () =>
        {
            return bufferFactory.Create(options.InitialBufferSize);
        });
        await pool.Initilize();
        var tempBuffer = ArrayPool<byte>.Shared.Rent(options.InitialBufferSize);

        var serializer = serializerFactory.Create(options.Connection.Protocol);
        var reclaimBufferAfterEachMessage = options.ReclaimBufferAfterEachMessage;
        try
        {
            var enumerable = ParallelMessageProcessorLoop(connection, options, pool, tempBuffer);
            await Parallel.ForEachAsync(enumerable, new ParallelOptions
            {
                MaxDegreeOfParallelism = 10,
                CancellationToken = connection.Context.RequestAborted
            }, async (buffer, token) =>
            {
                using var manager = buffer.Manager;
                await dispatcher.DispatchMessage(connection, serializer, manager.Memory, token);

                buffer.SetLength(0);

                if (reclaimBufferAfterEachMessage)
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

    private async IAsyncEnumerable<IMessageBuffer> ParallelMessageProcessorLoop(
        IWsConnection connection, WsMiddlewareOptions options, AsyncObjectPool<IMessageBuffer> pool, byte[] tempBuffer)
    {
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

            buffer ??= await pool.Rent();
            buffer.Write(tempBuffer.AsSpan(0, wsMessage.Count));

            if (wsMessage.EndOfMessage)
            {
                var result = buffer;
                buffer = null;
                yield return result;
            }
        }
    }

}
