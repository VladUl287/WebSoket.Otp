using System.Reflection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core;
using WebSockets.Otp.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddLogging();

    builder.Services.AddWsFramework(Assembly.GetExecutingAssembly());

    builder.Services.AddOpenApi();
}

var app = builder.Build();
{
    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider.InitializeWs();
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseWebSockets();

    app.Map("/ws", async (HttpContext ctx) =>
    {
        if (!ctx.WebSockets.IsWebSocketRequest)
        {
            ctx.Response.StatusCode = 400;
            return;
        }

        var socket = await ctx.WebSockets.AcceptWebSocketAsync();
        var conn = new WsConnection(Guid.NewGuid().ToString(), socket, ctx);
        var mgr = ctx.RequestServices.GetRequiredService<IWsConnectionManager>();
        if (!mgr.TryAdd(conn))
        {
            ctx.Response.StatusCode = 400;
            return;
        }

        var dispatcher = ctx.RequestServices.GetRequiredService<IMessageDispatcher>();
        var serializer = ctx.RequestServices.GetRequiredService<IMessageSerializer>();
        var buffer = new byte[8 * 1024];
        var ms = new MemoryStream();
        try
        {
            var token = ctx.RequestAborted;
            while (!token.IsCancellationRequested && socket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                {
                    await conn.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
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
                    await dispatcher.DispatchMessage(conn.AsPublicContext(), payload, token);
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            mgr.TryRemove(conn.Id);
            await conn.DisposeAsync();
        }
    });

    app.Run();
}