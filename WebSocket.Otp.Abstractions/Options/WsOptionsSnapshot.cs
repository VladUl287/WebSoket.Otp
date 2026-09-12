using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsOptionsSnapshot(WsOptions options)
{
    public IList<IAuthorizeData> AuthorizationData { get; init; } = options.AuthorizationData ?? [];
    public RequestDelegate? AuthPipeline { get; init; }

    public WebSocketOptions WebSocketOptions { get; init; } = options.WebSocketOptions ?? new();

    public int MaxDegreeOfParallelism { get; init; } = options.MaxDegreeOfParallelism;
    public TaskScheduler? TaskScheduler { get; init; } = options.TaskScheduler;

    public int MaxMessageSize { get; init; } = options.MaxMessageSize;
    public int ReceiveBufferSize { get; init; } = options.ReceiveBufferSize;
    public int BufferPoolSize { get; init; } = options.BufferPoolSize;
    public bool ShrinkBuffers { get; init; } = options.ShrinkBuffers;

    public Func<IGlobalContext, Task>? OnConnected { get; init; } = options.OnConnected;
    public Func<IGlobalContext, Task>? OnDisconnected { get; init; } = options.OnDisconnected;
}
