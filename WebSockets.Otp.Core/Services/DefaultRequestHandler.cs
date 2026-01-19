using WebSockets.Otp.Core.Logging;
using Microsoft.Extensions.Logging;
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
    IMessageProcessorResolver messageProcessorResolver, IMessageEnumerator messageEnumerator,
    IMessageReceiverResolver messageReceiverResolver,  ILogger<DefaultRequestHandler> logger) : IWsRequestHandler
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

        var messagesEnumerable = messageEnumerator.EnumerateAsync(messageReceiver, context, null, cancelToken);

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
            if (options.OnConnected is not null)
                await SafeExecuteAsync((state) => state.options.OnConnected!(state.globalContext),
                    (options, globalContext), "OnConnected", logger);

            var messageProcessor = messageProcessorResolver.Resolve(options.Processing.Mode);
            await messageProcessor.Process(
                context, globalContext, options, connectionOptions, cancelToken);
        }
        finally
        {
            connectionManager.TryRemove(connection.Id);

            if (options.OnDisconnected is not null)
                await SafeExecuteAsync((state) => state.options.OnDisconnected!(state.globalContext),
                    (options, globalContext), "OnDisconnected", logger);
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
