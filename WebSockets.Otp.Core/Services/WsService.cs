using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Contracts.Transport;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public sealed partial class WsService(
    IWsConnectionManager connectionManager, IWsConnectionFactory connectionFactory, IMessageProcessorFactory processorFactory,
    INewMessageProcessor messageProcessor, IMessageEnumerator messageEnumerator,
    IMessageReceiverResolver messageReceiver, ISerializerResolver serializerResolver, ILogger<WsService> logger) : IWsService
{
    public async Task HandleRequestAsync(ConnectionContext context, WsMiddlewareOptions options)
    {
        var connection = connectionFactory.Create(context.GetHttpContext(), null);

        if (!connectionManager.TryAdd(connection))
        {
            return;
        }

        if (!messageReceiver.TryResolve("json", out var textReceiver))
        {
            return;
        }

        var messages = messageEnumerator.EnumerateAsync(textReceiver, context, options, default);

        WsConnectionOptions? connectionOptions = null;
        await foreach (var handshake in messages)
        {
            connectionOptions = JsonSerializer.Deserialize<WsConnectionOptions>(
                handshake.Span,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            break;
        }

        try
        {
            if (options.OnConnected is not null)
                await SafeExecuteAsync((state) => state.options.OnConnected!(state.connection),
                    (options, connection), "OnConnected", logger);

            await messageProcessor.Process(context, connection, options, connectionOptions, default);
        }
        finally
        {
            connectionManager.TryRemove(connection.Id);

            if (options.OnDisconnected is not null)
                await SafeExecuteAsync((state) => state.options.OnDisconnected!(state.connection),
                    (options, connection), "OnDisconnected", logger);
        }
    }

    private static async Task SafeExecuteAsync<TState>(Func<TState, Task> action, TState state, string operationName, ILogger<WsService> logger)
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
