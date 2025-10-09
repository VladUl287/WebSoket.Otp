using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.AspNet.Options;

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

        //var maxMessageSize = options.MaxReceiveMessageSize;
        var socket = wsConnection.Socket;

        var buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
        var stream = new MemoryStream();
        try
        {
            var token = context.RequestAborted;
            while (socket.State is WebSocketState.Open && !token.IsCancellationRequested)
            {
                var wsMessage = await socket.ReceiveAsync(buffer, token);

                if (wsMessage.MessageType is WebSocketMessageType.Close)
                {
                    await wsConnection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                if (wsMessage.MessageType is WebSocketMessageType.Binary)
                    throw new NotSupportedException("Binary format message not supported yet.");

                //if (stream.Length + wsMessage.Count > maxMessageSize)
                //{
                //    await wsConnection.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message exceeds size limit", CancellationToken.None);
                //    break;
                //}
                
                await stream.WriteAsync(buffer.AsMemory(0, wsMessage.Count), token);

                if (wsMessage.EndOfMessage)
                {
                    var payload = stream.ToArray();
                    stream.SetLength(0);
                    await dispatcher.DispatchMessage(wsConnection, payload, token);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
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
