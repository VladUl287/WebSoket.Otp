using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Logging;

namespace WebSockets.Otp.AspNet;

public sealed partial class WsService(
    IWsConnectionManager connectionManager, IWsConnectionFactory connectionFactory, IMessageBufferFactory bufferFactory,
    IMessageDispatcher dispatcher, ILoggerFactory loggerFactory) : IWsService
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

            await ProcessMessagesAsync(connection, options);
        }
        finally
        {
            connectionManager.TryRemove(connection.Id);
            _logger.LogConnectionClosed(connection.Id);

            if (options.OnDisconnected is not null)
                await SafeExecuteAsync((conn) => options.OnDisconnected(conn), connection, "OnDisconnected", _logger);
        }
    }

    public async Task ProcessMessagesAsync(IWsConnection connection, WsMiddlewareOptions options)
    {
        var buffer = bufferFactory.Create(options.InitialBufferSize);
        var tempBuffer = ArrayPool<byte>.Shared.Rent(options.InitialBufferSize);
        try
        {
            var maxMessageSize = options.MaxMessageSize;
            var reclaimBufferAfterEachMessage = options.ReclaimBufferAfterEachMessage;

            var socket = connection.Socket;
            var token = connection.Context.RequestAborted;

            while (socket.State is WebSocketState.Open && !token.IsCancellationRequested)
            {
                var wsMessage = await socket.ReceiveAsync(tempBuffer, token);

                if (wsMessage.MessageType is WebSocketMessageType.Close)
                {
                    await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                if (wsMessage.MessageType is WebSocketMessageType.Binary)
                    throw new NotSupportedException("Binary format message not supported yet.");

                if (buffer.Length > maxMessageSize - wsMessage.Count)
                {
                    await connection.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message exceeds size limit", CancellationToken.None);
                    break;
                }

                buffer.Write(tempBuffer.AsSpan(0, wsMessage.Count));

                if (wsMessage.EndOfMessage)
                {
                    using (var manager = buffer.Manager)
                    {
                        await dispatcher.DispatchMessage(connection, manager.Memory, token);
                    }

                    buffer.SetLength(0);

                    if (reclaimBufferAfterEachMessage)
                        buffer.Shrink();
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
            buffer.Dispose();
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
