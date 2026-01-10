using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public sealed partial class WsService(
    IWsConnectionManager connectionManager, IWsConnectionFactory connectionFactory, IMessageProcessorFactory processorFactory,
    ILogger<WsService> logger) : IWsService
{
    public async Task HandleRequestAsync(HttpContext context, WsMiddlewareOptions options, WsConnectionOptions connectionOptions)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        var connection = connectionFactory.Create(context, webSocket);

        if (!connectionManager.TryAdd(connection))
        {
            logger.LogFailedToAddConnection(connection.Id);
            await connection.Socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Unable to register connection", CancellationToken.None);
            return;
        }

        try
        {
            logger.LogConnectionEstablished(connection.Id);

            if (options.OnConnected is not null)
                await SafeExecuteAsync((state) => state.options.OnConnected!(state.connection),
                    (options, connection), "OnConnected", logger);

            var processor = processorFactory.Create(options.Processing.Mode);

            await processor.Process(connection, options, connectionOptions);
        }
        finally
        {
            connectionManager.TryRemove(connection.Id);
            logger.LogConnectionClosed(connection.Id);

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
