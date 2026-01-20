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
        if (!readerStore.TryGet(_protocol, out var messageReader))
        {
            logger.LogError("");
            return null;
        }

        var messageEnumerator = enumeratorFactory.Create(context, messageReader);
        var messagesEnumerable = messageEnumerator.EnumerateAsync(bufferPool, token);

        var handshakeBuffer = await messagesEnumerable.FirstOrDefaultAsync(token);
        if (handshakeBuffer is null)
        {
            logger.LogError("");
            return null;
        }

        if (!serializerStore.TryGet(_protocol, out var serializer))
        {
            logger.LogError("");
            return null;
        }

        var handshakeOptions = serializer.Deserialize<WsHandshakeOptions>(handshakeBuffer.Span);
        if (handshakeOptions is null)
        {
            logger.LogError("");
            return null;
        }

        await bufferPool.Return(handshakeBuffer, token);

        await context.Transport.Output.WriteAsync(_responseBytes, token);

        return handshakeOptions;
    }
}
