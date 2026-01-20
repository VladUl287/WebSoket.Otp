using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeService(
    IMessageReaderStore readerStore, IMessageEnumeratorFactory enumeratorFactory, ISerializerStore serializerStore,
    IAsyncObjectPool<IMessageBuffer> bufferPool,
    ILogger<HandshakeService> logger) : IHandshakeService
{
    private static readonly string _protocol = "json";
    private static readonly ReadOnlyMemory<byte> _responseBytes = new byte[] { 0x7B, 0x7D, MessageConstants.JsonRecordSeparator }; //{}

    public async ValueTask<WsHandshakeOptions?> GetOptions(ConnectionContext context, CancellationToken token)
    {
        logger.HandshakeProcessStarted(context.ConnectionId);

        if (!readerStore.TryGet(_protocol, out var messageReader))
        {
            logger.HandshakeMessageReaderNotFound(_protocol, context.ConnectionId);
            return null;
        }

        logger.HandshakeMessageReaderObtained(_protocol, context.ConnectionId);

        var messageEnumerator = enumeratorFactory.Create(context, messageReader);
        var messagesEnumerable = messageEnumerator.EnumerateAsync(bufferPool, token);

        IMessageBuffer? handshakeBuffer = null;
        try
        {
            logger.HandshakeAwaitHandshakeMessage(context.ConnectionId);

            handshakeBuffer = await messagesEnumerable.FirstOrDefaultAsync(token);
            if (handshakeBuffer is null)
            {
                logger.HandshakeFailReadHandshakeMessage(context.ConnectionId);
                return null;
            }

            logger.HandshakeMessageReceived(handshakeBuffer.Length, context.ConnectionId);

            if (!serializerStore.TryGet(_protocol, out var serializer))
            {
                logger.HandshakeSerializerNotFound(_protocol, context.ConnectionId);
                return null;
            }

            logger.HandshakeSerializerObtained(_protocol, context.ConnectionId);

            var handshakeOptions = serializer.Deserialize<WsHandshakeOptions>(handshakeBuffer.Span);
            if (handshakeOptions is null)
            {
                logger.HandshakeDeserializeFailed(context.ConnectionId);
                return null;
            }

            logger.HandshakeStartResponseSending(context.ConnectionId);

            await context.Transport.Output
                .WriteAsync(_responseBytes, token);

            logger.HandshakeProcessFinished(context.ConnectionId);

            return handshakeOptions;
        }
        finally
        {
            if (handshakeBuffer is not null)
            {
                await bufferPool.Return(handshakeBuffer, token);
                logger.HandshakeBufferReturnedToPool(context.ConnectionId);
            }
        }
    }
}
