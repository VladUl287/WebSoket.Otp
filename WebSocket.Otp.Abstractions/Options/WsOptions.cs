using System.Text.RegularExpressions;

namespace WebSockets.Otp.Abstractions.Configuration;

public sealed class WsOptions : WsBaseOptions
{
    public StringComparer KeyComparer { get; set; } = StringComparer.OrdinalIgnoreCase;
    public bool KeyUnsafeIntern { get; set; } = false;
    public int KeyMaxLength { get; set; } = 1024;
    public int KeyMinLength { get; set; } = 1;
    public Regex? KeyPattern { get; set; }
}
