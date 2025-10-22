using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsMiddlewareOptions
{
    public string RequestPath { get; set; } = string.Empty;
    public string HandshakeRequestPath { get; set; } = string.Empty;

    public int MaxMessageSize { get; set; } = 64 * 1024; //64kb

    public int InitialBufferSize { get; set; } = 8 * 1024; // 8KB

    public bool ReclaimBufferAfterEachMessage { get; set; } = true;

    public TimeSpan ConnectionTokenLifeTime { get; set; } = TimeSpan.FromMinutes(1);

    public IWsRequestMatcher RequestMatcher { get; set; } = default!;

    public IWsRequestMatcher HandshakeRequestMatcher { get; set; } = default!;

    public WsAuthorizationOptions Authorization { get; set; } = new();

    public WsConnectionOptions Connection { get; set; } = new();

    public Func<IWsConnection, Task>? OnConnected { get; set; }
    public Func<IWsConnection, Task>? OnDisconnected { get; set; }
}
