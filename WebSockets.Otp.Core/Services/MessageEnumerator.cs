using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services;

public sealed class MessageEnumerator(IAsyncObjectPool<IMessageBuffer> bufferPool) : IMessageEnumerator
{
    public async IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
         WebSocket socket, WsOptionsSnapshot config, [EnumeratorCancellation] CancellationToken token)
    {
        var receiveBuffer = ArrayPool<byte>.Shared.Rent(config.ReceiveBufferSize);

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

        ArrayPool<byte>.Shared.Return(receiveBuffer);
    }

    public ValueTask ReturnBuffer(IMessageBuffer buffer, CancellationToken token) => bufferPool.Return(buffer, token);
}
