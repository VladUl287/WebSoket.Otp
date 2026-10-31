using System.Net.WebSockets;
using Microsoft.Extensions.ObjectPool;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services;

public sealed class MessageEnumerator(ObjectPool<IMessageBuffer> bufferPool) : IMessageEnumerator
{
    public async IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
         WebSocket socket, WsOptionsSnapshot config, [EnumeratorCancellation] CancellationToken token)
    {
        var receiveBuffer = GC.AllocateUninitializedArray<byte>(config.ReceiveBufferSize);
        var messageBuffer = bufferPool.Get();

        while (!token.IsCancellationRequested)
        {
            var receiveResult = await socket.ReceiveAsync(receiveBuffer, token);

            if (receiveResult is { MessageType: WebSocketMessageType.Close })
                break;

            if (receiveResult.Count > config.MaxMessageSize - messageBuffer.Length)
                throw new OutOfMemoryException($"Message exceed maximum message size '{config.MaxMessageSize}'.");

            messageBuffer.Write(receiveBuffer.AsSpan(0, receiveResult.Count));

            if (receiveResult.EndOfMessage)
            {
                yield return messageBuffer;
             
                messageBuffer = bufferPool.Get();
            }
        }
    }

    public void ReturnBuffer(IMessageBuffer buffer) => bufferPool.Return(buffer);
}
