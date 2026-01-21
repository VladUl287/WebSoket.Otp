using Microsoft.AspNetCore.Authorization;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;

namespace WebSockets.Otp.Abstractions.Configuration;

public class WsBaseConfiguration
{
    public IAuthorizeData[] AuthorizationData { get; set; } = [];

    public ProcessingMode ProcessingMode { get; set; } = ProcessingMode.Parallel;
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;
    public bool ShrinkBuffer { get; set; } = true;

    public Func<IGlobalContext, Task>? OnConnected { get; set; }
    public Func<IGlobalContext, Task>? OnDisconnected { get; set; }
}

