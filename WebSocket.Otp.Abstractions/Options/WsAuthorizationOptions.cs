using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsAuthorizationOptions
{
    public string RequestPath { get; set; } = string.Empty;

    public bool RequireAuthorization { get; set; }

    public string[] Schemes { get; set; } = [];
    public string[] Policies { get; set; } = [];
    public string[] Roles { get; set; } = [];

    public IWsRequestMatcher RequestMatcher { get; set; } = default!;
}
