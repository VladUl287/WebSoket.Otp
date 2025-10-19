using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Abstractions.Options;

public sealed class WsAuthorizationOptions
{
    public bool RequireAuthorization { get; set; } = true;

    public string[] Schemes { get; set; } = [];
    public string[] Policies { get; set; } = [];
    public string[] Roles { get; set; } = [];

    public Func<HttpContext, Task<bool>>? CustomValidation { get; set; }
}
