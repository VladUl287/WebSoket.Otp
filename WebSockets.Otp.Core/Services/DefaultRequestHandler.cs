using WebSockets.Otp.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;
using Microsoft.AspNetCore.Http.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Contracts.Transport;

namespace WebSockets.Otp.Core.Services;

public sealed partial class DefaultRequestHandler(
    IWsConnectionManager connectionManager, IWsConnectionFactory connectionFactory, 
    IHandshakeRequestParser handshakeRequestParser,
    INewMessageProcessor messageProcessor, IMessageEnumerator messageEnumerator,
    IMessageReceiverResolver messageReceiverResolver, ILogger<DefaultRequestHandler> logger) : IWsRequestHandler
{
    private const string DefaultHandshakeProtocol = "json";

    public async Task HandleRequestAsync(ConnectionContext context, WsMiddlewareOptions options)
    {
        var httpContext = context.GetHttpContext();
        ArgumentNullException.ThrowIfNull(httpContext);

        var cancelToken = httpContext.RequestAborted;

        if (!messageReceiverResolver.TryResolve(DefaultHandshakeProtocol, out var messageReceiver))
        {
            logger.LogError("Fail to resolve handshake protocol {DefaultHandshakeProtocol}.", DefaultHandshakeProtocol);
            return;
        }

        var messages = messageEnumerator.EnumerateAsync(messageReceiver, context, options, cancelToken);
        
        WsConnectionOptions? connectionOptions = null;
        await foreach (var handshakeMessage in messages)
        {
            connectionOptions = await handshakeRequestParser.Parse(handshakeMessage);
            break;
        }

        var transport = new DuplexPipeTransport(context.Transport);
        var connection = connectionFactory.Create(httpContext, transport);
        if (!connectionManager.TryAdd(connection))
            return;

        try
        {
            if (options.OnConnected is not null)
                await SafeExecuteAsync((state) => state.options.OnConnected!(state.connection),
                    (options, connection), "OnConnected", logger);

            await messageProcessor.Process(context, connection, options, connectionOptions, cancelToken);
        }
        finally
        {
            connectionManager.TryRemove(connection.Id);

            if (options.OnDisconnected is not null)
                await SafeExecuteAsync((state) => state.options.OnDisconnected!(state.connection),
                    (options, connection), "OnDisconnected", logger);
        }
    }

    private static async Task SafeExecuteAsync<TState>(Func<TState, Task> action, TState state, string operationName, ILogger<DefaultRequestHandler> logger)
    {
        try
        {
            await action(state);
        }
        catch (Exception ex)
        {
            logger.LogHandlerFail(operationName, ex);
        }
    }
}
