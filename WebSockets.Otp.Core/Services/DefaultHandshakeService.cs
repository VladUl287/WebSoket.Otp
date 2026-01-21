using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public sealed class DefaultHandshakeService(
    ISerializerStore serializerStore, IMessageBufferFactory bufferFactory,
    IMessageEnumerator enumerator, ILogger<DefaultHandshakeService> logger) : IHandshakeService
{
    private static readonly string _protocol = "json";
    private static readonly ReadOnlyMemory<byte> _responseBytes = "{}"u8.ToArray();

    public async ValueTask<WsHandshakeOptions?> ReceiveHandshakeOptions(
        HttpContext context, WebSocket socket, WsBaseConfiguration options, CancellationToken token)
    {
        logger.HandshakeProcessStarted(context);

        var messagesEnumerable = enumerator.EnumerateAsync(socket, options, bufferFactory, token);

        logger.HandshakeAwaitHandshakeMessage(context);

        using var handshakeBuffer = await messagesEnumerable.FirstOrDefaultAsync(token);
        if (handshakeBuffer is null)
        {
            logger.HandshakeFailReadHandshakeMessage(context);
            return null;
        }

        logger.HandshakeMessageReceived(handshakeBuffer.Length, context);

        if (!serializerStore.TryGet(_protocol, out var serializer))
        {
            logger.HandshakeSerializerNotFound(_protocol, context);
            return null;
        }

        logger.HandshakeSerializerObtained(_protocol, context);

        var handshakeOptions = serializer.Deserialize<WsHandshakeOptions>(handshakeBuffer.Span);
        if (handshakeOptions is null)
        {
            logger.HandshakeDeserializeFailed(context);
            return null;
        }

        logger.HandshakeStartResponseSending(context);

        await socket.SendAsync(_responseBytes, WebSocketMessageType.Text, true, token);

        logger.HandshakeProcessFinished(context);

        return handshakeOptions;
    }
}
