using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class WsConnection(string id, HttpContext context, WebSocket socket) : IWsConnection
{
    public string Id => id;

    public HttpContext Context => context;

    public WebSocket Socket => socket;

    public async Task SendAsync(ReadOnlyMemory<byte> payload, WebSocketMessageType type, CancellationToken token) =>
        await socket.SendAsync(payload, type, true, token);

    public Task CloseAsync(WebSocketCloseStatus status, string? description, CancellationToken token) =>
        socket.CloseAsync(status, description, token);

    public void Dispose() => socket.Dispose();
}
