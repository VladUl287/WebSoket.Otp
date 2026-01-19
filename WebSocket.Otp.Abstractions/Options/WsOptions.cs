using System.Text.RegularExpressions;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsOptions : WsBaseOptions
{
    public int MessageMaxSize { get; set; } = 64 * 1024; // 64KB
    public int MessageBufferCapacity { get; set; } = 4 * 1024; // 4KB
    public int MessageBufferPoolSize { get; set; } = 1024;

    public WsEndpointKeyOptions KeyOptions { get; init; } = new();
}

public sealed class WsEndpointKeyOptions
{
    public bool InternUnsafeMode { get; init; } = false;
    public bool IgnoreCase { get; set; } = false;
    public int MaxLength { get; set; } = 256;
    public int MinLength { get; set; } = 1;
    public Regex? Pattern { get; set; }
}