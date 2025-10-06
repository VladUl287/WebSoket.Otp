using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class WsConnection(string id, WebSocket socket, HttpContext context) : IWsConnection
{
    public string Id => id;

    public HttpContext? Context => context;

    public WebSocket? Transport => socket;

    public string Path => context.Request.Path;

    public string? SubProtocol => socket.SubProtocol;

    public IDictionary<string, object> Items => new ConcurrentDictionary<string, object>();

    public ValueTask SendAsync(ReadOnlyMemory<byte> payload, WebSocketMessageType type, CancellationToken token) =>
        socket.SendAsync(payload, type, true, token);

    public Task CloseAsync(WebSocketCloseStatus status, string? description, CancellationToken token) =>
        socket.CloseAsync(status, description, token);

    public void Dispose() => socket.Dispose();
}
