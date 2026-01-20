using Microsoft.Extensions.Logging;
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
    IMessageReaderStore readerStore, IMessageEnumeratorFactory enumeratorFactory, IAsyncObjectPool<IMessageBuffer> bufferPool,
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

        var token = httpContext.RequestAborted;

        var handshakeOptions = await hanshakeService.GetOptions(context, token);
        if (handshakeOptions is null)
        {
            logger.LogError("");
            return;
        }

        if (!readerStore.TryGet(handshakeOptions.Protocol, out var messageReader))
        {
            logger.LogError("");
            return;
        }

        if (!serializerStore.TryGet(handshakeOptions.Protocol, out var serializer))
        {
            logger.LogError("");
            return;
        }

        var messageEnumerator = enumeratorFactory.Create(context, messageReader);
        var duplectPipeTransport = new DuplexPipeTransport(context.Transport);
        var connection = connectionFactory.Create(duplectPipeTransport);

        if (!connectionManager.TryAdd(connection))
        {
            logger.LogError("");
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
