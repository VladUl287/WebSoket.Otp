using WebSockets.Otp.Core.Extensions;
using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Contracts;
using Microsoft.AspNetCore.Http.Connections;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services;

public sealed partial class DefaultRequestHandler(
    IWsConnectionManager connectionManager, IWsConnectionFactory connectionFactory,
    IHandshakeParser handshakeRequestParser, IExecutionContextFactory executionContextFactory,
    IMessageProcessorResolver messageProcessorResolver, IMessageEnumerator messageEnumerator,
    IMessageReceiverResolver messageReceiverResolver, IAsyncObjectPoolFactory poolFactory,
    IMessageEnumeratorFactory enumeratorFactory, IMessageBufferFactory bufferFactory) : IWsRequestHandler
{
    private const string DefaultHandshakeProtocol = "json";

    public async Task HandleRequestAsync(ConnectionContext context, WsMiddlewareOptions options)
    {
        var httpContext = context.GetHttpContext();

        ArgumentNullException.ThrowIfNull(httpContext);

        var cancelToken = httpContext.RequestAborted;

        if (!messageReceiverResolver.TryResolve(DefaultHandshakeProtocol, out var messageReceiver))
        {
            return;
        }

        await using var bufferPool = poolFactory.Create(options.Memory.MaxBufferPoolSize, () =>
        {
            return bufferFactory.Create(options.Memory.InitialBufferSize);
        });

        var messagesEnumerable = messageEnumerator.EnumerateAsync(context, messageReceiver, bufferPool, cancelToken);

        var handshakeMessage = await messagesEnumerable.FirstOrDefaultAsync(cancelToken);
        if (handshakeMessage is null)
        {
            return;
        }

        if (!handshakeRequestParser.TryParse(handshakeMessage, out var connectionOptions))
        {
            return;
        }

        var duplectPipeTransport = new DuplexPipeTransport(context.Transport);
        var connection = connectionFactory.Create(duplectPipeTransport);

        if (!connectionManager.TryAdd(connection))
        {
            return;
        }

        var globalContext = executionContextFactory.CreateGlobal(httpContext, connection.Id, connectionManager);
        try
        {
            options.OnConnected?.Invoke(globalContext);

            var messageProcessor = messageProcessorResolver.Resolve(options.ProcessingMode);

            await messageProcessor.Process(null, null, null, null, null, default);
        }
        finally
        {
            connectionManager.TryRemove(connection.Id);

            options.OnDisconnected?.Invoke(globalContext);
        }
    }
}
