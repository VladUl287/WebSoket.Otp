using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsMiddlewareOptions
{
    public string RequestPath { get; set; } = string.Empty;
    public string HandshakeRequestPath { get; set; } = string.Empty;

    public int MaxMessageSize { get; set; } = 64 * 1024; //64kb
    public int InitialBufferSize { get; set; } = 8 * 1024; // 8KB

    public string ProcessingMode { get; set; } = MessageProcessingModes.Sequential;
    public int MaxParallelProcessingPerConnection { get; set; } = 5;

    public bool ReclaimBufferAfterEachMessage { get; set; } = true;

    public IWsRequestMatcher RequestMatcher { get; set; } = default!;

    public IWsRequestMatcher HandshakeRequestMatcher { get; set; } = default!;

    public WsAuthorizationOptions Authorization { get; set; } = new();

    public WsConnectionOptions? Connection { get; set; }

    public Func<IWsConnection, Task>? OnConnected { get; set; }
    public Func<IWsConnection, Task>? OnDisconnected { get; set; }
}

public static class MessageProcessingModes
{
    public const string Sequential = "sequential";
    public const string Parallel = "parallel";
    //public const string Priorities = "sequential";
    //public const string Batch = "sequential";
    //public const string Throttled = "sequential";
}