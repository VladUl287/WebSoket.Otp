using System.Security.Claims;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsConnectionOptions
{
    public ClaimsPrincipal? User { get; set; }
    public string Protocol { get; set; } = "JSON";
    public DateTime CreatedAt { get; init; }
}

