using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsConfiguration
{
    public WsConfiguration(WsOptions options)
    {
        AuthorizationData = options.AuthorizationData ?? [];
        WebSocketOptions = options.WebSocketOptions ?? new();
        ProcessingMode = options.ProcessingMode;
        MaxDegreeOfParallelism = options.MaxDegreeOfParallelism;
        TaskScheduler = options.TaskScheduler;
        MaxMessageSize = options.MaxMessageSize;
        ReceiveBufferSize = options.ReceiveBufferSize;
        BufferPoolSize = options.BufferPoolSize;
        ShrinkBuffers = options.ShrinkBuffers;
        OnConnected = options.OnConnected;
        OnDisconnected = options.OnDisconnected;
    }

    public IList<IAuthorizeData> AuthorizationData { get; init; }
    public WebSocketOptions WebSocketOptions { get; init; }

    public ProcessingMode ProcessingMode { get; init; }
    public int MaxDegreeOfParallelism { get; init; }
    public TaskScheduler? TaskScheduler { get; init; }

    public int MaxMessageSize { get; init; }
    public int ReceiveBufferSize { get; init; }
    public int BufferPoolSize { get; init; }
    public bool ShrinkBuffers { get; init; }

    public Func<IGlobalContext, Task>? OnConnected { get; init; }
    public Func<IGlobalContext, Task>? OnDisconnected { get; init; }
}
