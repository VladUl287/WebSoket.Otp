namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsAuthorizationOptions
{
    public string? Policy { get; set; }
    public string[]? Schemes { get; set; }
    public string[]? Roles { get; set; }
}
