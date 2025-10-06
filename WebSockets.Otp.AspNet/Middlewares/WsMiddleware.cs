using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.AspNet.Options;
using WebSockets.Otp.Core;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.AspNet.Middlewares;

public sealed class WsMiddleware(RequestDelegate next, WsMiddlewareOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest && Appropriate(context, options))
        {
            await ExecuteWebSocket(context);
            return;
        }
        
        await next(context);
    }

    private static bool Appropriate(HttpContext context, WsMiddlewareOptions options) => context.Request.Path.Equals(options.Path);

    private static async Task ExecuteWebSocket(HttpContext context)
    {
        var socket = await context.WebSockets.AcceptWebSocketAsync();

        var idProvider = context.RequestServices.GetRequiredService<IIdProvider>();
        var wsConnection = new WsConnection(idProvider.NewId(), socket, context);

        var manager = context.RequestServices.GetRequiredService<IWsConnectionManager>();
        if (!manager.TryAdd(wsConnection))
        {
            context.Response.StatusCode = 400;
            return;
        }

        var dispatcher = context.RequestServices.GetRequiredService<IMessageDispatcher>();
        var buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
        var ms = new MemoryStream();
        try
        {
            var token = context.RequestAborted;
            while (!token.IsCancellationRequested && socket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                {
                    await wsConnection.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
                    break;
                }

                if (result.Count > 0)
                {
                    ms.Write(buffer, 0, result.Count);
                }

                if (result.EndOfMessage)
                {
                    var payload = ms.ToArray();
                    ms.SetLength(0);
                    await dispatcher.DispatchMessage(wsConnection.AsPublicContext(), payload, token);
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            manager.TryRemove(wsConnection.Id);
            wsConnection.Dispose();
        }
    }
}
