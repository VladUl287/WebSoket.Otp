using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public sealed class DefaultHandshakeService(
    IMessageEnumeratorFactory enumeratorFactory, ISerializerStore serializerStore,
    IAsyncObjectPool<IMessageBuffer> bufferPool, ILogger<DefaultHandshakeService> logger) : IHandshakeService
{
    private static readonly string _protocol = "json";
    private static readonly ReadOnlyMemory<byte> _responseBytes = "{}"u8.ToArray(); //{}

    public async ValueTask<WsHandshakeOptions?> ReceiveHandshakeOptions(WebSocket socket, CancellationToken token)
    {
        logger.HandshakeProcessStarted(string.Empty);

        var messageEnumerator = enumeratorFactory.Create(socket);
        var messagesEnumerable = messageEnumerator.EnumerateAsync(bufferPool, token);

        IMessageBuffer? handshakeBuffer = null;
        try
        {
            logger.HandshakeAwaitHandshakeMessage(string.Empty);

            handshakeBuffer = await messagesEnumerable.FirstOrDefaultAsync(token);
            if (handshakeBuffer is null)
            {
                logger.HandshakeFailReadHandshakeMessage(string.Empty);
                return null;
            }

            logger.HandshakeMessageReceived(handshakeBuffer.Length, string.Empty);

            if (!serializerStore.TryGet(_protocol, out var serializer))
            {
                logger.HandshakeSerializerNotFound(_protocol, string.Empty);
                return null;
            }

            logger.HandshakeSerializerObtained(_protocol, string.Empty);

            var handshakeOptions = serializer.Deserialize<WsHandshakeOptions>(handshakeBuffer.Span);
            if (handshakeOptions is null)
            {
                logger.HandshakeDeserializeFailed(string.Empty);
                return null;
            }

            logger.HandshakeStartResponseSending(string.Empty);

            await socket.SendAsync(_responseBytes, WebSocketMessageType.Text, true, token);

            logger.HandshakeProcessFinished(string.Empty);

            return handshakeOptions;
        }
        finally
        {
            if (handshakeBuffer is not null)
            {
                await bufferPool.Return(handshakeBuffer, token);
                logger.HandshakeBufferReturnedToPool(string.Empty);
            }
        }
    }
}
