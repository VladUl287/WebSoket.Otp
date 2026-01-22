using System.Text.RegularExpressions;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsGlobalOptions : WsOptions
{
    public KeyOptions Keys { get; set; } = new();

    public class KeyOptions
    {
        public StringComparer Comparer { get; set; } = StringComparer.OrdinalIgnoreCase;
        public int MinLength { get; set; } = 1;
        public int MaxLength { get; set; } = 1024;
        public Regex? Pattern { get; set; }
        public bool UnsafeIntern { get; set; } = false;
    }
}
