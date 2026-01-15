using Microsoft.AspNetCore.Authorization;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsMiddlewareOptions
{
    public AuthorizeAttribute? Authorization { get; set; }

    public WsMemoryManagementOptions Memory { get; set; } = new();
    public WsMessageProcessingOptions Processing { get; set; } = new();

    public Func<IGlobalExecutionContext, Task>? OnConnected { get; set; }
    public Func<IGlobalExecutionContext, Task>? OnDisconnected { get; set; }
}

