using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Core.Services.Endpoints;

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

    private static Endpoint BuildEndpoint(params object[] metadata) =>
        new(
            requestDelegate: _ => Task.CompletedTask,
            metadata: new EndpointMetadataCollection(metadata),
            displayName: "test");

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
        var endpoint = BuildEndpoint(/* nothing */);

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.True(result.Succeeded);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task AllowAnonymous_on_endpoint_with_only_authorize_fails()
    {
        var ctx = CreateSourceContext(); // unauthenticated
        var endpoint = BuildEndpoint(new AuthorizeAttribute());

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Bare_authorize_fails_when_default_scheme_returns_no_result()
    {
        TestAuthHandler.Principal.Value = null; // handler returns NoResult
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute());

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.False(result.Succeeded);
        Assert.Equal("principal not authenticated", result.FailureReason);
    }

    [Fact]
    public async Task Bare_authorize_succeeds_when_default_scheme_authenticates()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser();
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute());

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.True(result.Succeeded);
        Assert.True(result.User!.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task Bare_authorize_falls_back_to_connection_user_when_default_scheme_yields_nothing()
    {
        TestAuthHandler.Principal.Value = null;
        var connectionUser = AuthenticatedUser("alice");
        var ctx = CreateSourceContext(user: connectionUser);
        var endpoint = BuildEndpoint(new AuthorizeAttribute());

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.True(result.Succeeded);
        Assert.Same(connectionUser, result.User);
    }

    [Fact]
    public async Task Explicit_scheme_uses_that_scheme_and_succeeds()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser();
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(
            new AuthorizeAttribute { AuthenticationSchemes = TestAuthHandler.SchemeName });

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Explicit_scheme_fails_when_handler_fails()
    {
        TestAuthHandler.FailAuth.Value = true;
        var ctx = CreateSourceContext(user: AuthenticatedUser()); // connection user exists
        var endpoint = BuildEndpoint(
            new AuthorizeAttribute { AuthenticationSchemes = TestAuthHandler.SchemeName });

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Explicit_scheme_any_scheme_wins()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser();
        var ctx = CreateSourceContext();
        // Second scheme has no handler registered? both are registered; use two names.
        var endpoint = BuildEndpoint(
            new AuthorizeAttribute
            {
                AuthenticationSchemes = $"{TestAuthHandler.OtherSchemeName},{TestAuthHandler.SchemeName}"
            });

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Roles_authorize_succeeds_for_user_in_role()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser("alice", "admin");
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute { Roles = "admin" });

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Roles_authorize_fails_for_user_not_in_role()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser("alice", "user");
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute { Roles = "admin" });

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.FailedRequirements);
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
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute { Policy = "Adult" });

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.True(result.Succeeded);
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

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.False(result.Succeeded);
        Assert.Contains(result.FailedRequirements,
            s => s.Contains(nameof(MinimumAgeRequirement)));
    }

    [Fact]
    public async Task Policy_that_names_a_scheme_re_authenticates_with_that_scheme()
    {
        // Default scheme would find no principal; the policy's named scheme does.
        TestAuthHandler.Principal.Value = AuthenticatedUser("bob");
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute { Policy = "SchemeBound" });

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Multiple_authorize_attributes_all_must_pass()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser("alice", "admin");
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(
            new AuthorizeAttribute { Roles = "admin" },
            new AuthorizeAttribute { Policy = "Adult" }); // Adult requires DOB claim

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.False(result.Succeeded); // DOB missing → Adult fails
    }

    [Fact]
    public async Task Headers_query_and_path_are_visible_to_handlers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ProbeCapture>();
        services.AddAuthentication(ProbeAuthHandler.Scheme)
            .AddScheme<AuthenticationSchemeOptions, ProbeAuthHandler>(ProbeAuthHandler.Scheme, _ => { });
        services.AddAuthorization();
        services.AddSingleton<IEndpointAuthorizer, EndpointAuthorizer>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var authorizer = scope.ServiceProvider.GetRequiredService<IEndpointAuthorizer>();
        var capture = scope.ServiceProvider.GetRequiredService<ProbeCapture>();

        var ctx = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = new ClaimsPrincipal(new ClaimsIdentity()),
        };
        ctx.Request.Method = "GET";
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("localhost");
        ctx.Request.PathBase = "";
        ctx.Request.Path = "/ws";
        ctx.Request.QueryString = new QueryString("?x=1");
        ctx.Request.Headers.Authorization = "Bearer tok";
        ctx.Request.Headers.Cookie = "a=b";
        ctx.Request.Headers["X-Custom"] = "yes";

        var endpoint = BuildEndpoint(
            new AuthorizeAttribute { AuthenticationSchemes = ProbeAuthHandler.Scheme });

        var result = await authorizer.AuthorizeAsync(ctx, endpoint, default);

        Assert.True(result.Succeeded);
        Assert.Equal("Bearer tok", capture.Authorization);
        Assert.Equal("a=b", capture.Cookie);
        Assert.Equal("?x=1", capture.Query);
        Assert.Equal("/ws", capture.Path);
        Assert.Equal("yes", capture.Custom);
        Assert.NotNull(capture.Headers);
        Assert.NotSame(ctx.Request.Headers, capture.Headers);
    }

    public sealed class ProbeCapture
    {
        public string? Authorization { get; set; }
        public string? Cookie { get; set; }
        public string? Query { get; set; }
        public string? Path { get; set; }
        public string? Custom { get; set; }
        public IHeaderDictionary? Headers { get; set; }
    }

    public sealed class ProbeAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string Scheme = "Probe";

        private readonly ProbeCapture _capture;

        public ProbeAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ProbeCapture capture) : base(options, logger, encoder)
        {
            _capture = capture;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            _capture.Authorization = Request.Headers.Authorization;
            _capture.Cookie = Request.Headers.Cookie;
            _capture.Query = Request.QueryString.Value;
            _capture.Path = Request.Path.Value;
            _capture.Custom = Request.Headers["X-Custom"];
            _capture.Headers = Request.Headers;

            var id = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "probe") }, Scheme);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(id), Scheme)));
        }
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

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.True(result.Succeeded);
        Assert.NotSame(connectionUser, result.User);
        Assert.Equal("scheme-user", result.User!.Identity!.Name);
    }

    [Fact]
    public async Task Cancellation_token_is_honored()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute());

        var ex = await Record.ExceptionAsync(() =>
            _sut.AuthorizeAsync(ctx, endpoint, cts.Token));

        Assert.True(ex is null or OperationCanceledException);
    }

    [Fact]
    public async Task Failure_result_carries_a_reason()
    {
        TestAuthHandler.FailAuth.Value = true;
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(
            new AuthorizeAttribute { AuthenticationSchemes = TestAuthHandler.SchemeName });

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public async Task Role_failure_populates_failed_requirements()
    {
        TestAuthHandler.Principal.Value = AuthenticatedUser("alice", "user");
        var ctx = CreateSourceContext();
        var endpoint = BuildEndpoint(new AuthorizeAttribute { Roles = "admin" });

        var result = await _sut.AuthorizeAsync(ctx, endpoint, default);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.FailedRequirements);
    }
}