using Microsoft.AspNetCore.Authorization;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsMiddlewareOptions
{
    [StringSyntax("Route")]
    public string RequestPath { get; set; } = "/ws";

    [StringSyntax("Route")]
    public string HandshakePath { get; set; } = "/ws/_handshake";

    public AuthorizeAttribute? Authorization { get; set; }

    public WsMemoryManagementOptions Memory { get; set; } = new();
    public WsMessageProcessingOptions Processing { get; set; } = new();

    public Func<IWsConnection, Task>? OnConnected { get; set; }
    public Func<IWsConnection, Task>? OnDisconnected { get; set; }
}

