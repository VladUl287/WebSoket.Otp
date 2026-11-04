using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Tests.Services.Auth;

public sealed class EndpointAuthorizerTests : IClassFixture<EndpointAuthorizerFixture>
{
    private readonly EndpointAuthorizerFixture _fixture;
    private readonly IEndpointAuthorizer _sut;

    public EndpointAuthorizerTests(EndpointAuthorizerFixture fixture)
    {
        _fixture = fixture;
        _sut = _fixture.Services.GetRequiredService<IEndpointAuthorizer>();

        TestAuthHandler.Principal.Value = null;
        TestAuthHandler.FailAuth.Value = false;
        TestAuthHandler.TokenInHeader.Value = null;
    }

    private HttpContext CreateSourceContext(
        ClaimsPrincipal? user = null,
        string? bearerToken = null,
        string? cookie = null,
        Action<HttpContext>? configure = null)
    {
        var ctx = new DefaultHttpContext
        {
            RequestServices = _fixture.Services.CreateScope().ServiceProvider,
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity()),
        };
        ctx.Request.Method = "GET";
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("localhost");
        ctx.Request.Path = "/ws";
        ctx.Request.PathBase = "";

        if (bearerToken is not null)
            ctx.Request.Headers.Authorization = "Bearer " + bearerToken;
        if (cookie is not null)
            ctx.Request.Headers.Cookie = cookie;

        configure?.Invoke(ctx);
        return ctx;
    }

    private static WsEndpointInfo BuildEndpoint(params object[] metadata)
    {
        return new WsEndpointInfo
        {
            EndpointType = typeof(object),
            Key = "",
            Invoker = null,
            Endpoint = new(
                requestDelegate: _ => Task.CompletedTask,
                metadata: new EndpointMetadataCollection(metadata),
                displayName: "test")
        };
    }

    private static ClaimsPrincipal AuthenticatedUser(string name = "alice", params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, name) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    [Fact]
    public async Task No_authorize_metadata_succeeds()
    {
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint();

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AllowAnonymous_on_endpoint_with_only_authorize_fails()
    {
        var ctx = CreateSourceContext(); // unauthenticated
        var endpoint = BuildEndpoint(new AuthorizeAttribute());

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Bare_authorize_fails_when_default_scheme_returns_no_result()
    {
        TestAuthHandler.Principal.Value = null; // handler returns NoResult
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute());

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.False(result.IsSuccess);
        Assert.Equal("authorization failed", result.Error);
    }

    [Fact]
    public async Task Bare_authorize_succeeds_when_default_scheme_authenticates()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser();
        var ctx = CreateSourceContext(TestAuthHandler.Principal.Value);
        var endpoint = BuildEndpoint(new AuthorizeAttribute());

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Bare_authorize_falls_back_to_connection_user_when_default_scheme_yields_nothing()
    {
        TestAuthHandler.Principal.Value = null;
        var connectionUser = AuthenticatedUser("alice");
        var ctx = CreateSourceContext(user: connectionUser);
        var endpoint = BuildEndpoint(new AuthorizeAttribute());

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Explicit_scheme_uses_that_scheme_and_succeeds()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser();
        var ctx = CreateSourceContext(TestAuthHandler.Principal.Value);
        var endpoint = BuildEndpoint(
            new AuthorizeAttribute { AuthenticationSchemes = TestAuthHandler.SchemeName });

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Explicit_scheme_any_scheme_wins()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser();
        var ctx = CreateSourceContext(TestAuthHandler.Principal.Value);
        var endpoint = BuildEndpoint(
            new AuthorizeAttribute
            {
                AuthenticationSchemes = $"{TestAuthHandler.OtherSchemeName},{TestAuthHandler.SchemeName}"
            });

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Roles_authorize_succeeds_for_user_in_role()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser("alice", "admin");
        var ctx = CreateSourceContext(TestAuthHandler.Principal.Value);
        var endpoint = BuildEndpoint(new AuthorizeAttribute { Roles = "admin" });

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Roles_authorize_fails_for_user_not_in_role()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser("alice", "user");
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute { Roles = "admin" });

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public async Task Policy_succeeds_when_requirement_met()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "alice"),
            new Claim(ClaimTypes.DateOfBirth, DateTime.UtcNow.AddYears(-30).ToString("o")),
        }, "Test"));
        TestAuthHandler.Principal.Value = user;
        var ctx = CreateSourceContext(user);
        var endpoint = BuildEndpoint(new AuthorizeAttribute { Policy = "Adult" });

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Policy_fails_when_requirement_not_met()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "kid"),
            new Claim(ClaimTypes.DateOfBirth, DateTime.UtcNow.AddYears(-10).ToString("o")),
        }, "Test"));
        TestAuthHandler.Principal.Value = user;
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute { Policy = "Adult" });

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public async Task Policy_that_names_a_scheme_re_authenticates_with_that_scheme()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser("bob");
        var ctx = CreateSourceContext(TestAuthHandler.Principal.Value);
        var endpoint = BuildEndpoint(new AuthorizeAttribute { Policy = "SchemeBound" });

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Multiple_authorize_attributes_all_must_pass()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser("alice", "admin");
        var ctx = CreateSourceContext(TestAuthHandler.Principal.Value);
        var endpoint = BuildEndpoint(
            new AuthorizeAttribute { Roles = "admin" },
            new AuthorizeAttribute { Policy = "Adult" }); // Adult requires DOB claim

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.False(result.IsSuccess); // DOB missing → Adult fails
    }

    [Fact]
    public async Task Returned_user_is_the_re_authenticated_principal_not_the_connection_user()
    {
        var connectionUser = AuthenticatedUser("connection-user");
        var schemeUser = AuthenticatedUser("scheme-user");
        TestAuthHandler.Principal.Value = schemeUser;

        var ctx = CreateSourceContext(user: connectionUser);
        var endpoint = BuildEndpoint(
            new AuthorizeAttribute { AuthenticationSchemes = TestAuthHandler.SchemeName });

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Cancellation_token_is_honored()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute());

        var ex = await Record.ExceptionAsync(() =>
            _sut.AuthorizeAsync(ctx, endpoint));

        Assert.True(ex is null or OperationCanceledException);
    }

    [Fact]
    public async Task Failure_result_carries_a_reason()
    {
        TestAuthHandler.FailAuth.Value = true;
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(
            new AuthorizeAttribute { AuthenticationSchemes = TestAuthHandler.SchemeName });

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Role_failure_populates_failed_requirements()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser("alice", "user");
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute { Roles = "admin" });

        var result = await _sut.AuthorizeAsync(ctx, endpoint);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Error);
    }
}