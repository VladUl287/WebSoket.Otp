using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.AspNet;

public sealed class WsService(
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
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning("Failed to add connection {ConnectionId} to connection manager", connection.Id);

            await connection.CloseAsync(WebSocketCloseStatus.InternalServerError, "Unable to register connection", CancellationToken.None);
            return;
        }

        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("WebSocket connection established: {ConnectionId}", connection.Id);

            if (options.OnConnected is not null)
                await SafeExecuteAsync(() => options.OnConnected(connection), "OnConnected", _logger);

        }
        finally
        {
            connectionManager.TryRemove(connection.Id);
            _logger.LogInformation("WebSocket connection closed: {ConnectionId}", connection.Id);

            if (options.OnDisconnected is not null)
                await SafeExecuteAsync(() => options.OnDisconnected(connection), "OnDisconnected", _logger);
        }
    }

    private static async Task SafeExecuteAsync(Func<Task> action, string operationName, ILogger<WsService> logger)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
                logger.LogError(ex, "Error executing {OperationName} handler", operationName);
        }
    }
}
