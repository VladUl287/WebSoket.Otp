using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services;

public sealed class MessageEnumerator(ArrayPool<byte> arrayPool) : IMessageEnumerator
{
    public async IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
         WebSocket socket, WsOptionsSnapshot config, IAsyncObjectPool<IMessageBuffer> bufferPool,
         [EnumeratorCancellation] CancellationToken token)
    {
        var receiveBuffer = arrayPool.Rent(config.ReceiveBufferSize);

        IMessageBuffer? messageBuffer = null;
        while (!token.IsCancellationRequested)
        {
            messageBuffer ??= await bufferPool.Rent(token);

            var receiveResult = await socket.ReceiveAsync(receiveBuffer, token);

            if (receiveResult is { MessageType: WebSocketMessageType.Close })
                break;

            if (receiveResult.Count > config.MaxMessageSize - messageBuffer.Length)
                throw new OutOfMemoryException($"Message exceed maximum message size '{config.MaxMessageSize}'.");

            messageBuffer.Write(receiveBuffer.AsSpan(0, receiveResult.Count));

            if (receiveResult.EndOfMessage)
            {
                var completedBuffer = messageBuffer;
                messageBuffer = null;

                yield return completedBuffer;
            }
        }

        arrayPool.Return(receiveBuffer);
    }
}
