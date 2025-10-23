using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsMiddlewareOptions
{
    public PathString RequestPath { get; set; } = "/ws";
    public PathString HandshakeRequestPath { get; set; } = "/ws/_handshake";

    public MemorySettings Memory { get; set; } = new();

    public string ProcessingMode { get; set; } = MessageProcessingModes.Sequential;
    public int MaxParallelProcessingPerConnection { get; set; } = 5;

    public IWsRequestMatcher RequestMatcher { get; set; } = default!;

    public IWsRequestMatcher HandshakeRequestMatcher { get; set; } = default!;

    public WsAuthorizationOptions Authorization { get; set; } = new();

    public WsConnectionOptions? Connection { get; set; }

    public Func<IWsConnection, Task>? OnConnected { get; set; }
    public Func<IWsConnection, Task>? OnDisconnected { get; set; }
}

public sealed class MemorySettings
{
    public int MaxMessageSize { get; set; } = 64 * 1024; // 64KB
    public int InitialBufferSize { get; set; } = 8 * 1024; // 8KB
    public bool ReclaimBuffersImmediately { get; set; } = true;
    public int MaxBufferPoolSize { get; set; } = 10;
}

public static class MessageProcessingModes
{
    public const string Sequential = "sequential";
    public const string Parallel = "parallel";
    //public const string Priorities = "sequential";
    //public const string Batch = "sequential";
    //public const string Throttled = "sequential";
}