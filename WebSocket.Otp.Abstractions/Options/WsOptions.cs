using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsOptions
{
    public IList<IAuthorizeData> AuthorizationData { get; set; } = [];

    public WebSocketOptions WebSocketOptions { get; set; } = new();

    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    public TaskScheduler? TaskScheduler { get; set; }

    public int MaxMessageSize { get; set; } = 4 * 1024 * 1024; // 4MB
    public int ReceiveBufferSize { get; set; } = 4 * 1024; // 4KB
    public int BufferPoolSize { get; set; } = 1024;
    public bool ShrinkBuffers { get; set; } = true;

    public Func<IGlobalContext, Task>? OnConnected { get; set; }
    public Func<IGlobalContext, Task>? OnDisconnected { get; set; }
}
