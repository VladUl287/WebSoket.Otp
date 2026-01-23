using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Logging;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services;

public sealed class DefaultHandshakeHandler(
    ISerializerStore store, IMessageEnumerator enumerator, IAsyncObjectPool<IMessageBuffer> objectPool,
    ILogger<DefaultHandshakeHandler> logger) : IHandshakeHandler
{
    private static readonly string _protocol = "json";
    private static readonly ReadOnlyMemory<byte> _responseBytes = "{}"u8.ToArray();

    public async ValueTask<WsHandshakeOptions?> HandleAsync(
        HttpContext context, WebSocket socket, WsConfiguration options, CancellationToken token)
    {
        var traceId = new TraceId(context);

        logger.HandshakeProcessStarted(traceId);

        var messagesEnumerable = enumerator.EnumerateAsync(socket, options, objectPool, token);

        logger.HandshakeAwaitHandshakeMessage(traceId);

        using var handshakeBuffer = await messagesEnumerable.FirstOrDefaultAsync(token);
        if (handshakeBuffer is null)
        {
            logger.HandshakeFailReadHandshakeMessage(traceId);
            return null;
        }

        logger.HandshakeMessageReceived(handshakeBuffer.Length, context);

        if (!store.TryGet(_protocol, out var serializer))
        {
            logger.HandshakeSerializerNotFound(_protocol, traceId);
            return null;
        }

        logger.HandshakeSerializerObtained(_protocol, traceId);

        var handshakeOptions = (WsHandshakeOptions?)serializer.Deserialize(typeof(WsHandshakeOptions), handshakeBuffer.Span);
        if (handshakeOptions is null)
        {
            logger.HandshakeDeserializeFailed(traceId);
            return null;
        }

        logger.HandshakeStartResponseSending(traceId);

        await socket.SendAsync(_responseBytes, WebSocketMessageType.Text, true, token);

        logger.HandshakeProcessFinished(traceId);

        return handshakeOptions;
    }
}
