using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Logging;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public sealed partial class DefaultRequestHandler(
    IConnectionManager connectionManager, IWsConnectionFactory connectionFactory, IHandshakeService hanshakeService,
    IExecutionContextFactory executionContextFactory, IMessageProcessorStore processorResolver, ISerializerStore serializerStore,
    IMessageReaderStore readerStore, IMessageEnumeratorFactory enumeratorFactory, ILogger<DefaultRequestHandler> logger) : IRequestHandler
{
    public async Task HandleRequestAsync(ConnectionContext context, WsBaseOptions options)
    {
        logger.RequestProcessingStarted();

        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            logger.HttpContextNotFound();
            return;
        }

        var token = httpContext.RequestAborted;
        var handshakeOptions = await hanshakeService.GetOptions(context, token);
        if (handshakeOptions is null)
        {
            logger.HandshakeOptionsNotFound();
            return;
        }

        logger.HandshakeCompleted(handshakeOptions.Protocol);

        if (!readerStore.TryGet(handshakeOptions.Protocol, out var messageReader))
        {
            logger.MessageReaderNotFound(handshakeOptions.Protocol);
            return;
        }

        if (!serializerStore.TryGet(handshakeOptions.Protocol, out var serializer))
        {
            logger.SerializerNotFound(handshakeOptions.Protocol);
            return;
        }

        var transport = connectionFactory.CreateTransport(context.Transport, serializer);
        var connection = connectionFactory.Create(transport);

        if (!connectionManager.TryAdd(connection))
        {
            logger.ConnectionAddFailed(connection.Id);
            return;
        }

        logger.ConnectionEstablished(connection.Id);

        var globalContext = executionContextFactory.CreateGlobal(httpContext, connection.Id, connectionManager);
        try
        {
            logger.InvokingOnConnectedCallback(connection.Id);
            options.OnConnected?.Invoke(globalContext);

            var messageProcessor = processorResolver.Get(options.ProcessingMode);
            var messageEnumerator = enumeratorFactory.Create(context, messageReader);

            logger.MessageProcessingStarted(connection.Id, options.ProcessingMode);

            await messageProcessor.Process(messageEnumerator, globalContext, serializer, options, default);

            logger.MessageProcessingCompleted(connection.Id);
        }
        finally
        {
            logger.RemovingConnection(connection.Id);
            connectionManager.TryRemove(connection.Id);

            logger.InvokingOnDisconnectedCallback(connection.Id);
            options.OnDisconnected?.Invoke(globalContext);

            logger.ConnectionClosed(connection.Id);
        }
    }
}
