using System.Security.Claims;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsConnectionOptions
{
    public IEnumerable<Claim>? Claims { get; set; }
    public string Protocol { get; set; } = "JSON";
}

