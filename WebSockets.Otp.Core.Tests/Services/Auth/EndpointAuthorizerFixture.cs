using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Core.Services.Endpoints;

namespace WebSockets.Otp.Core.Tests.Services.Auth;

public sealed class EndpointAuthorizerFixture : IDisposable
{
    public ServiceProvider Services { get; }

    public EndpointAuthorizerFixture()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
            o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
        })
        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
            TestAuthHandler.SchemeName, _ => { })
        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
            TestAuthHandler.OtherSchemeName, _ => { });

        services.AddAuthorization(o =>
        {
            o.AddPolicy("Adult", p => p
                .RequireAuthenticatedUser()
                .AddRequirements(new MinimumAgeRequirement(18)));

            o.AddPolicy("AdminOnly", p => p
                .RequireAuthenticatedUser()
                .RequireRole("admin"));

            o.AddPolicy("SchemeBound", p =>
            {
                p.RequireAuthenticatedUser();
                p.AddAuthenticationSchemes(TestAuthHandler.OtherSchemeName);
            });
        });

        services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
        services.AddSingleton<IEndpointAuthorizer, EndpointAuthorizer>();

        Services = services.BuildServiceProvider();
    }

    public void Dispose() => Services.Dispose();
}

