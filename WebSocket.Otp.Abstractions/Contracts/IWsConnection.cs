using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Security.Claims;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnection : IAsyncDisposable
{
    string Id { get; }

    string Path { get; }

    string? SubProtocol { get; }

    ClaimsPrincipal? User { get; }

    HttpContext? Context { get; }

    IDictionary<string, object> Items { get; }

    WebSocket? Transport { get; }

    ValueTask SendAsync(ReadOnlyMemory<byte> payload, WebSocketMessageType type, CancellationToken toke);

    Task CloseAsync(WebSocketCloseStatus status, string? description, CancellationToken token);
}
