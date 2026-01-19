using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.Core.Services;

public sealed partial class DefaultRequestHandler(
    IWsConnectionManager connectionManager, IWsConnectionFactory connectionFactory,
    IHandshakeService handshakeRequestParser, IExecutionContextFactory executionContextFactory,
    IMessageProcessorResolver messageProcessorResolver, ISerializerResolver serializerResolver,
    IMessageReceiverResolver messageReceiverResolver, IMessageEnumeratorFactory enumeratorFactory,
    IAsyncObjectPool<IMessageBuffer> bufferPool) : IWsRequestHandler
{
    public async Task HandleRequestAsync(ConnectionContext context, WsBaseOptions options)
    {
        var httpContext = context.GetHttpContext() ?? throw new NullReferenceException();

        var cancellationToken = httpContext.RequestAborted;

        if (!messageReceiverResolver.TryResolve(handshakeRequestParser.ProtocolName, out var messageReceiver))
        {
            return;
        }

        var messageEnumerator = enumeratorFactory.Create(context, messageReceiver);
        var messagesEnumerable = messageEnumerator.EnumerateAsync(bufferPool, cancellationToken);

        var handshakeMessage = await messagesEnumerable.FirstOrDefaultAsync(cancellationToken);
        if (handshakeMessage is null)
        {
            return;
        }

        if (!handshakeRequestParser.TryParse(handshakeMessage, out var connectionOptions))
        {
            return;
        }

        await context.Transport.Output
            .WriteAsync(handshakeRequestParser.SuccessResponseBytes, cancellationToken);

        if (!messageReceiverResolver.TryResolve(connectionOptions.Protocol, out messageReceiver))
        {
            return;
        }

        messageEnumerator = enumeratorFactory.Create(context, messageReceiver);

        var duplectPipeTransport = new DuplexPipeTransport(context.Transport);
        var connection = connectionFactory.Create(duplectPipeTransport);

        if (!connectionManager.TryAdd(connection))
        {
            return;
        }

        if (!serializerResolver.TryResolve(connectionOptions.Protocol, out var serializer))
        {
            return;
        }

        var globalContext = executionContextFactory.CreateGlobal(httpContext, connection.Id, connectionManager);
        try
        {
            options.OnConnected?.Invoke(globalContext);

            var messageProcessor = messageProcessorResolver.Resolve(options.ProcessingMode);

            await messageProcessor.Process(messageEnumerator, globalContext, serializer, options, default);
        }
        finally
        {
            connectionManager.TryRemove(connection.Id);

            options.OnDisconnected?.Invoke(globalContext);
        }
    }
}
