using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.AspNet.Options;
using WebSockets.Otp.Core.Helpers;

namespace WebSockets.Otp.AspNet.Middlewares;

public sealed class WsMiddleware(RequestDelegate next, WsMiddlewareOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (options.RequestMatcher.IsWebSocketRequest(context))
            return AcceptWebSocket(context, options);

        return next(context);
    }

    private static async Task AcceptWebSocket(HttpContext context, WsMiddlewareOptions options)
    {
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        await ListenSocket(context, socket, options);
    }

    private static async Task ListenSocket(HttpContext context, WebSocket socket, WsMiddlewareOptions options)
    {
        var connectionManager = context.RequestServices.GetRequiredService<IWsConnectionManager>();
        using var wsConnection = RegisterConnection(context, socket, connectionManager);
        try
        {
            await SocketLoop(context, wsConnection, options);
        }
        finally
        {
            connectionManager.TryRemove(wsConnection.Id);
        }
    }

    public static async Task SocketLoop(HttpContext context, IWsConnection wsConnection, WsMiddlewareOptions options)
    {
        var dispatcher = context.RequestServices.GetRequiredService<IMessageDispatcher>();

        var maxMessageSize = options.MaxMessageSize;
        var socket = wsConnection.Socket;

        var buffer = new NativeChunkedBuffer(options.InitialBufferSize);
        var tempBuffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
        try
        {
            var token = context.RequestAborted;
            while (socket.State is WebSocketState.Open && !token.IsCancellationRequested)
            {
                var wsMessage = await socket.ReceiveAsync(tempBuffer, token);

                if (wsMessage.MessageType is WebSocketMessageType.Close)
                {
                    await wsConnection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                if (wsMessage.MessageType is WebSocketMessageType.Binary)
                    throw new NotSupportedException("Binary format message not supported yet.");

                if (buffer.Length + wsMessage.Count > maxMessageSize)
                {
                    await wsConnection.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message exceeds size limit", CancellationToken.None);
                    break;
                }

                buffer.Write(tempBuffer.AsSpan(0, wsMessage.Count));

                if (wsMessage.EndOfMessage)
                {
                    using (var manager = buffer.Manager)
                    {
                        await dispatcher.DispatchMessage(wsConnection, manager.Memory, token);
                    }

                    buffer.SetLength(0);

                    if (options.ReclaimBufferAfterEachMessage)
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

    private static IWsConnection RegisterConnection(HttpContext context, WebSocket socket, IWsConnectionManager manager)
    {
        var factory = context.RequestServices.GetRequiredService<IWsConnectionFactory>();

        var wsConnection = factory.Create(context, socket);
        if (manager.TryAdd(wsConnection))
            return wsConnection;

        throw new Exception("Fail to add connection into connection manager.");
    }
}
