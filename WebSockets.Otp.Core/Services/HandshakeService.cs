using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeService(
    IMessageReaderStore readerStore, IMessageEnumeratorFactory enumeratorFactory, ISerializerStore serializerStore,
    IAsyncObjectPool<IMessageBuffer> bufferPool,
    ILogger<HandshakeService> logger) : IHandshakeService
{
    private static readonly string _protocol = "json";
    private static readonly byte[] _responseBytes = [0x7B, 0x7D, MessageConstants.JsonRecordSeparator]; //{}

    public async Task<WsHandshakeOptions?> GetOptions(ConnectionContext context, CancellationToken token)
    {
        logger.LogDebug("Starting handshake process for connection: {ConnectionId}", context.ConnectionId);

        if (!readerStore.TryGet(_protocol, out var messageReader))
        {
            logger.LogError("No message reader found for protocol '{Protocol}' during handshake", _protocol);
            return null;
        }

        logger.LogDebug("Message reader obtained for protocol '{Protocol}'", _protocol);

        var messageEnumerator = enumeratorFactory.Create(context, messageReader);
        var messagesEnumerable = messageEnumerator.EnumerateAsync(bufferPool, token);

        IMessageBuffer? handshakeBuffer = null;
        try
        {
            logger.LogDebug("Starting await handshake message for connection: {ConnectionId}", context.ConnectionId);

            handshakeBuffer = await messagesEnumerable.FirstOrDefaultAsync(token);
            if (handshakeBuffer is null)
            {
                logger.LogError("Failed to read handshake message from connection: {ConnectionId}", context.ConnectionId);
                return null;
            }

            logger.LogDebug("Handshake message received ({Length} bytes)", handshakeBuffer.Length);

            if (!serializerStore.TryGet(_protocol, out var serializer))
            {
                logger.LogError("No serializer found for protocol '{Protocol}' during handshake", _protocol);
                return null;
            }

            logger.LogDebug("Serializer obtained for protocol '{Protocol}'", _protocol);

            var handshakeOptions = serializer.Deserialize<WsHandshakeOptions>(handshakeBuffer.Span);
            if (handshakeOptions is null)
            {
                logger.LogError("Failed to deserialize handshake options from connection: {ConnectionId}", context.ConnectionId);
                return null;
            }

            logger.LogDebug("Handshake processing complete for connection: {ConnectionId}", context.ConnectionId);

            logger.LogDebug("Start sending handshake response to connection: {ConnectionId}", context.ConnectionId);

            await context.Transport.Output
                .WriteAsync(_responseBytes, token);

            logger.LogDebug("Handshake successful for connection: {ConnectionId} with options: {HandshakeOptions}",
                context.ConnectionId, handshakeOptions);

            return handshakeOptions;
        }
        finally
        {
            if (handshakeBuffer is not null)
            {
                await bufferPool.Return(handshakeBuffer, token);
                logger.LogTrace("Buffer returned to pool. Finish handshake processing.");
            }
        }
    }
}
