using Microsoft.AspNetCore.Authorization;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;

namespace WebSockets.Otp.Abstractions.Configuration;

public class WsBaseOptions
{
    public IAuthorizeData[] AuthorizationData { get; set; } = [];

    public ProcessingMode ProcessingMode { get; set; } = ProcessingMode.Parallel;
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;
    public bool ShrinkBuffer { get; set; } = true;

    public int MaxMessageSize { get; set; } = 64 * 1024; // 64KB
    public int MessageBufferCapacity { get; set; } = 4 * 1024; // 4KB
    public int MessageBufferPoolSize { get; set; } = 1024;

    public Func<IGlobalContext, Task>? OnConnected { get; set; }
    public Func<IGlobalContext, Task>? OnDisconnected { get; set; }
}

