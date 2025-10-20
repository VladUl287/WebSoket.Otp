namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsAuthorizationOptions
{
    public bool RequireAuthorization { get; set; }

    public string[] Schemes { get; set; } = [];
    public string[] Policies { get; set; } = [];
    public string[] Roles { get; set; } = [];
}
