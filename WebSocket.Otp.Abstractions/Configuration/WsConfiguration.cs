using System.Text.RegularExpressions;

namespace WebSockets.Otp.Abstractions.Configuration;

public sealed class WsConfiguration : WsBaseConfiguration
{
    public EndpointKeyOptions Endpoint { get; init; } = new();
}

public sealed class EndpointKeyOptions
{
    public bool UnsafeInternKeys { get; set; } = false;
    public StringComparer Comparer { get; set; } = StringComparer.OrdinalIgnoreCase;
    public int MaxLength { get; set; } = 1024;
    public int MinLength { get; set; } = 1;
    public Regex? Pattern { get; set; }
}