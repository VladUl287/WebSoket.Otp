using Microsoft.AspNetCore.Authorization;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsMiddlewareOptions
{
    public AuthorizeAttribute? Authorization { get; set; }

    public string ProcessingMode { get; set; } = Options.ProcessingMode.Parallel;
    public int ProcessingMaxDegreeOfParallelilism { get; set; } = Environment.ProcessorCount * 10;

    public WsMemoryManagementOptions Memory { get; set; } = new();

    public Func<IGlobalContext, Task>? OnConnected { get; set; }
    public Func<IGlobalContext, Task>? OnDisconnected { get; set; }
}

