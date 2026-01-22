using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;

namespace WebSockets.Otp.Abstractions.Options;

public class WsOptions
{
    public IList<IAuthorizeData> AuthorizationData { get; set; } = [];

    public WebSocketOptions WebSocketOptions { get; set; } = new();

    public ProcessingMode ProcessingMode { get; set; } = ProcessingMode.Parallel;
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    public TaskScheduler? TaskScheduler { get; set; }

    public int MaxMessageSize { get; set; } = 64 * 1024; // 64KB
    public int ReceiveBufferSize { get; set; } = 4 * 1024; // 4KB
    public int BufferPoolSize { get; set; } = 1024;
    public bool ShrinkBuffers { get; set; } = true;

    public Func<IGlobalContext, Task>? OnConnected { get; set; }
    public Func<IGlobalContext, Task>? OnDisconnected { get; set; }
}

