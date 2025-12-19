using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsMiddlewareOptions
{
    public PathSettings Paths { get; set; } = new();
    public MemorySettings Memory { get; set; } = new();
    public ProcessingSettings Processing { get; set; } = new();
    public WsAuthorizationOptions Authorization { get; set; } = new();
    public WsConnectionOptions Connection { get; set; } = new();

    public Func<IWsConnection, Task>? OnConnected { get; set; }
    public Func<IWsConnection, Task>? OnDisconnected { get; set; }
}

