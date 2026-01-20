using System.Text.RegularExpressions;

namespace WebSockets.Otp.Abstractions.Configuration;

public sealed class WsConfiguration : WsBaseConfiguration
{
    public int MaxMessageSize { get; set; } = 64 * 1024; // 64KB
    public int MessageBufferCapacity { get; set; } = 4 * 1024; // 4KB
    public int MessageBufferPoolSize { get; set; } = 1024;

    public EndpointKeyOptions Endpoint { get; init; } = new();
}

public sealed class EndpointKeyOptions
{
    public bool UnsafeInternKeys { get; set; } = false;
    public bool IgnoreCase { get; set; } = false;
    public int MaxLength { get; set; } = 1024;
    public int MinLength { get; set; } = 1;
    public Regex? Pattern { get; set; }
}