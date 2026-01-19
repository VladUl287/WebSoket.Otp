using WebSockets.Otp.Core.Extensions;
using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Contracts;
using Microsoft.AspNetCore.Http.Connections;

namespace WebSockets.Otp.Core.Services;

public sealed partial class DefaultRequestHandler(
    IWsConnectionManager connectionManager, IWsConnectionFactory connectionFactory,
    IHandshakeParser handshakeRequestParser, IExecutionContextFactory executionContextFactory,
    IMessageProcessorResolver messageProcessorResolver, ISerializerResolver serializerResolver,
    IMessageReceiverResolver messageReceiverResolver, IMessageEnumeratorFactory enumeratorFactory) : IWsRequestHandler
{
    private const string DefaultHandshakeProtocol = "json";

    public async Task HandleRequestAsync(ConnectionContext context, WsMiddlewareOptions options)
    {
        var httpContext = context.GetHttpContext() ?? throw new NullReferenceException();

        var cancellationToken = httpContext.RequestAborted;

        if (!messageReceiverResolver.TryResolve(DefaultHandshakeProtocol, out var messageReceiver))
        {
            return;
        }

        var messageEnumerator = enumeratorFactory.Create(context, messageReceiver);
        var messagesEnumerable = messageEnumerator.EnumerateAsync(cancellationToken);

        var handshakeMessage = await messagesEnumerable.FirstOrDefaultAsync(cancellationToken);
        if (handshakeMessage is null)
        {
            return;
        }

        if (!handshakeRequestParser.TryParse(handshakeMessage, out var connectionOptions))
        {
            return;
        }

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
