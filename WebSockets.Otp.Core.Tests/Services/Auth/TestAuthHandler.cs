using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace WebSockets.Otp.Core.Tests.Services.Auth;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string OtherSchemeName = "Test2";

    public static readonly AsyncLocal<ClaimsPrincipal?> Principal = new();
    public static readonly AsyncLocal<string?> TokenInHeader = new();
    public static readonly AsyncLocal<bool> FailAuth = new();

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (FailAuth.Value)
            return Task.FromResult(AuthenticateResult.Fail("forced failure"));

        var principal = Principal.Value;
        if (principal is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int Age { get; }
    public MinimumAgeRequirement(int age) => Age = age;
}

public sealed class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        var dob = context.User.FindFirst(ClaimTypes.DateOfBirth)?.Value;
        if (dob is not null &&
            DateTime.TryParse(dob, out var parsed) &&
            (DateTime.UtcNow - parsed).TotalDays / 365.25 >= requirement.Age)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
