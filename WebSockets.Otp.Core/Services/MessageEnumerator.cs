using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services;

public sealed class MessageEnumerator : IMessageEnumerator
{
    public async IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
         WebSocket socket, WsBaseOptions options, IMessageBufferFactory bufferFactory,
         [EnumeratorCancellation] CancellationToken token)
    {
        var tempBuffer = ArrayPool<byte>.Shared.Rent(4096);
        var tempMemory = tempBuffer.AsMemory();

        try
        {
            var buffer = bufferFactory.Create(options.MessageBufferCapacity);

            while (!token.IsCancellationRequested)
            {
                var receiveResult = await socket.ReceiveAsync(tempMemory, token);

                if (receiveResult is { MessageType: WebSocketMessageType.Close })
                {
                    break;
                }

                buffer.Write(tempMemory.Span[..receiveResult.Count]);

                if (receiveResult.EndOfMessage)
                {
                    var resultBuffer = buffer;
                    buffer = bufferFactory.Create(options.MessageBufferCapacity);
                    yield return resultBuffer;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
        }
    }

    public async IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
         WebSocket socket, WsBaseOptions options, IAsyncObjectPool<IMessageBuffer> bufferPool,
         [EnumeratorCancellation] CancellationToken token)
    {
        var tempBuffer = ArrayPool<byte>.Shared.Rent(4096);
        var tempMemory = tempBuffer.AsMemory();

        try
        {
            IMessageBuffer? buffer = null;
            while (!token.IsCancellationRequested)
            {
                buffer ??= await bufferPool.Rent(token);

                var receiveResult = await socket.ReceiveAsync(tempMemory, token);

                if (receiveResult is { MessageType: WebSocketMessageType.Close })
                {
                    break;
                }

                buffer.Write(tempMemory.Span[..receiveResult.Count]);

                if (receiveResult.EndOfMessage)
                {
                    var resultBuffer = buffer;
                    buffer = null;
                    yield return resultBuffer;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
        }
    }
}
