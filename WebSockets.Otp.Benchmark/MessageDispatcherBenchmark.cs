using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Services.Utils;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
public class MessageDispatcherBenchmark
{
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private IMessageDispatcher _dispatcher = null!;
    private IGlobalContext _globalContext = null!;
    private IMessageSerializer _serializer = null!;
    private IMessageBuffer _buffer = null!;

    private static readonly byte[] Frame = Encoding.UTF8.GetBytes(
        "{\"key\":\"echo1\",\"correlationId\":12324,\"value\":{\"msg\":\"hello\"}}");

    [GlobalSetup]
    public async Task Setup()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.None);
        });
        services.AddWsEndpoints();

        services.AddRouting();
        services.AddAuthorization();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("secretsecretsecretsecretsecretsecretsecretsecretsecret")),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.Equals("/ws"))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
            });

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        _scope = _provider.CreateScope();
        var scopedProvider = _scope.ServiceProvider;

        _dispatcher = scopedProvider.GetRequiredService<IMessageDispatcher>();
        var contextFactory = scopedProvider.GetRequiredService<IContextFactory>();
        var options = scopedProvider.GetRequiredService<WsOptions>();
        var ctx = Create(scopedProvider);
        _globalContext = contextFactory.CreateGlobal(ctx, await ctx.WebSockets.AcceptWebSocketAsync(), "test", new WsOptionsSnapshot(options));
        var store = scopedProvider.GetRequiredService<ISerializerStore>();
        store.TryGet("json", out _serializer);

        _buffer = new NativeChunkedBuffer(Frame.Length);
        _buffer.Write(Frame);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
        _provider.Dispose();
        _buffer.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task Dispatch_HappyPath()
    {
        await _dispatcher.DispatchMessage(_globalContext, _serializer, _buffer, CancellationToken.None);
    }

    public static DefaultHttpContext Create(
        IServiceProvider services,
        string path = "/ws",
        string method = "GET")
    {
        var ctx = new DefaultHttpContext { RequestServices = services };

        ctx.Request.Method = method;
        ctx.Request.Path = path;
        ctx.Request.Scheme = "http";
        ctx.Request.Host = new HostString("localhost", 5000);
        ctx.Request.QueryString = new QueryString("?access_token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxIiwidW5pcXVlX25hbWUiOiJkZWZhdWx0Iiwicm9sZSI6IlVzZXIiLCJzY29wZSI6IndzLmNoYXQiLCJuYmYiOjE3OTA1ODA0MzQsImV4cCI6MTc5MTQ0NDQzNCwiaWF0IjoxNzkwNTgwNDM0fQ.0xc2gLPD1XXWFjRNnVWdhe3S0xlyxgIndBQgRDeWA64");

        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "test-user")
        ], "Test"));

        ctx.Features.Set<IHttpWebSocketFeature>(new FakeWebSocketFeature(new InMemoryWebSocket()));
        return ctx;
    }
}

[Authorize]
[WsEndpoint("echo")]
public class EndpointTest : WsEndpoint<object>
{
    public override Task HandleAsync(object request, EndpointContext context)
    {
        return Task.CompletedTask;
    }
}

public sealed class FakeWebSocketFeature : IHttpWebSocketFeature
{
    private readonly WebSocket _socket;
    public FakeWebSocketFeature(WebSocket socket) => _socket = socket;

    public bool IsWebSocketRequest { get; set; } = true;
    public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context) => Task.FromResult(_socket);
}

public sealed class InMemoryWebSocket : WebSocket
{
    public override WebSocketCloseStatus? CloseStatus => null;

    public override string? CloseStatusDescription => null;

    public override WebSocketState State => WebSocketState.Open;

    public override string? SubProtocol => null;

    public override void Abort()
    {
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
    }

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WebSocketReceiveResult(1, WebSocketMessageType.Text, true));
    }

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}