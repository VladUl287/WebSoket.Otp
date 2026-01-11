using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services;

public sealed class MessageReceiver : IMessageReceiver
{
    public async ValueTask<IMessageBuffer> Receive(WebSocket webSocket, WsMiddlewareOptions options, IMessageBuffer buffer, CancellationToken token)
    {
        using var tempBuffer = MemoryPool<byte>.Shared.Rent(options.Memory.InitialBufferSize);

        while (webSocket.State is WebSocketState.Open && !token.IsCancellationRequested)
        {
            var wsMessage = await webSocket.ReceiveAsync(tempBuffer.Memory, token);

            if (wsMessage.MessageType is WebSocketMessageType.Close)
                break;

            buffer.Write(tempBuffer.Memory.Span[..wsMessage.Count]);

            if (wsMessage.EndOfMessage)
                break;
        }

        return buffer;
    }
}
