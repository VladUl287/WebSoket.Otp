using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Logging;

namespace WebSockets.Otp.AspNet;

public sealed partial class WsService(
    IWsConnectionManager connectionManager, IWsConnectionFactory connectionFactory, IMessageBufferFactory bufferFactory,
    IMessageDispatcher messageDispatcher, ILoggerFactory loggerFactory) : IWsService
{
    private readonly ILogger<WsService> _logger = loggerFactory.CreateLogger<WsService>();

    public async Task HandleWebSocketRequestAsync(HttpContext context, WsMiddlewareOptions options)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await AcceptSocket(context, webSocket, options);
    }

    private async Task AcceptSocket(HttpContext context, WebSocket webSocket, WsMiddlewareOptions options)
    {
        var connection = connectionFactory.Create(context, webSocket);

        if (!connectionManager.TryAdd(connection))
        {
            _logger.LogFailedToAddConnection(connection.Id);
            await connection.CloseAsync(WebSocketCloseStatus.InternalServerError, "Unable to register connection", CancellationToken.None);
            return;
        }

        try
        {
            _logger.LogConnectionEstablished(connection.Id);

            if (options.OnConnected is not null)
                await SafeExecuteAsync((conn) => options.OnConnected(conn), connection, "OnConnected", _logger);

        }
        finally
        {
            connectionManager.TryRemove(connection.Id);
            _logger.LogConnectionClosed(connection.Id);

            if (options.OnDisconnected is not null)
                await SafeExecuteAsync((conn) => options.OnDisconnected(conn), connection, "OnDisconnected", _logger);
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
