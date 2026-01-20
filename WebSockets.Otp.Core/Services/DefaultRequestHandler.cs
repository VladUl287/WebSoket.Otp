using Microsoft.Extensions.Logging;
using WebSockets.Otp.Core.Extensions;
using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Abstractions.Options;
using Microsoft.AspNetCore.Http.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services;

public sealed partial class DefaultRequestHandler(
    IWsConnectionManager connectionManager, IWsConnectionFactory connectionFactory, IHandshakeService hanshakeService,
    IExecutionContextFactory executionContextFactory, IMessageProcessorStore processorResolver, ISerializerStore serializerStore,
    IMessageReaderStore messageReaderStore, IMessageEnumeratorFactory enumeratorFactory, IAsyncObjectPool<IMessageBuffer> bufferPool,
    ILogger<DefaultRequestHandler> logger) : IWsRequestHandler
{
    public async Task HandleRequestAsync(ConnectionContext context, WsBaseOptions options)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            logger.LogError("");
            return;
        }

        if (!messageReaderStore.TryGet(hanshakeService.Protocol, out var messageReader))
        {
            logger.LogError("");
            return;
        }

        var token = httpContext.RequestAborted;

        var messageEnumerator = enumeratorFactory.Create(context, messageReader);
        var messagesEnumerable = messageEnumerator.EnumerateAsync(bufferPool, token);

        var handshakeBuffer = await messagesEnumerable.FirstOrDefaultAsync(token);
        if (handshakeBuffer is null)
        {
            logger.LogError("");
            return;
        }

        if (!serializerStore.TryGet(hanshakeService.Protocol, out var serializer))
        {
            logger.LogError("");
            return;
        }

        var handshakeOptions = serializer.Deserialize<WsHandshakeOptions>(handshakeBuffer.Span);
        if (handshakeOptions is null)
        {
            logger.LogError("");
            return;
        }

        await bufferPool.Return(handshakeBuffer, token);

        await context.Transport.Output
            .WriteAsync(hanshakeService.ResponseBytes, token);

        if (!messageReaderStore.TryGet(handshakeOptions.Protocol, out messageReader))
        {
            return;
        }

        messageEnumerator = enumeratorFactory.Create(context, messageReader);

        var duplectPipeTransport = new DuplexPipeTransport(context.Transport);
        var connection = connectionFactory.Create(duplectPipeTransport);

        if (!connectionManager.TryAdd(connection))
        {
            return;
        }

        if (!serializerStore.TryGet(handshakeOptions.Protocol, out serializer))
        {
            return;
        }

        var globalContext = executionContextFactory.CreateGlobal(httpContext, connection.Id, connectionManager);
        try
        {
            options.OnConnected?.Invoke(globalContext);

            var messageProcessor = processorResolver.Get(options.ProcessingMode);
            await messageProcessor.Process(messageEnumerator, globalContext, serializer, options, default);
        }
        finally
        {
            connectionManager.TryRemove(connection.Id);

            options.OnDisconnected?.Invoke(globalContext);
        }
    }
}
