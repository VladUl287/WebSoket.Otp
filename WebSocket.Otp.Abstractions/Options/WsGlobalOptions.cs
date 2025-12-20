using System.Text.RegularExpressions;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsGlobalOptions
{
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