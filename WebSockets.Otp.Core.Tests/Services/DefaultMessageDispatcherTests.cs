using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Buffers;
using System.Net.WebSockets;
using System.Security.Claims;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Models;
using WebSockets.Otp.Core.Services;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Tests.Services;

public class DefaultMessageDispatcherTests
{
    private sealed class FakeMessageBuffer : IMessageBuffer
    {
        private readonly byte[] _buffer;
        private int _length;

        public FakeMessageBuffer(params byte[] data)
        {
            _buffer = new byte[Math.Max(data.Length, 16)];
            Array.Copy(data, _buffer, data.Length);
            _length = data.Length;
        }

        public int Length => _length;
        public int Capacity => _buffer.Length;
        public Span<byte> Span => _buffer.AsSpan(0, _length);
        public Memory<byte> Memory => _buffer.AsMemory(0, _length);
        public void Write(ReadOnlySpan<byte> data) => throw new NotSupportedException();
        public void Write(ReadOnlySequence<byte> data) => throw new NotSupportedException();
        public void SetLength(int length) => _length = length;
        public void Shrink() { }
        public void Dispose() { }
    }

    private sealed class FakeSerializer : ISerializer
    {
        public string Protocol => "test";
        public WebSocketMessageType Type => WebSocketMessageType.Text;

        public bool TryGetFieldValueIndexResult { get; set; }
        public int FieldIndex { get; set; }

        public byte[]? LastData { get; private set; }
        public string? LastField { get; private set; }
        public int TryGetFieldCallCount { get; private set; }

        public ReadOnlyMemory<byte> Serialize<T>(T message) => default;
        public T? Deserialize<T>(ReadOnlySpan<byte> data) => default;

        public bool TryGetFieldValueIndex(ReadOnlySpan<byte> data, string field, out int index)
        {
            TryGetFieldCallCount++;
            LastData = data.ToArray();
            LastField = field;
            index = FieldIndex;
            return TryGetFieldValueIndexResult;
        }
    }

    private sealed class FakeEndpointResolver : ITrieResolver<WsEndpointInfo>
    {
        public bool Result { get; set; }
        public WsEndpointInfo? Value { get; set; }
        public byte[]? LastSequence { get; private set; }
        public int CallCount { get; private set; }

        public bool TryResolve(ReadOnlySpan<byte> sequence, out WsEndpointInfo? value)
        {
            CallCount++;
            LastSequence = sequence.ToArray();
            value = Value;
            return Result;
        }
    }

    private sealed class FakeEndpointInvoker : IEndpointInvoker
    {
        public object? Endpoint { get; private set; }
        public IEndpointContext? Context { get; private set; }
        public int CallCount { get; private set; }

        public Task Invoke(object endpoint, IEndpointContext ctx)
        {
            CallCount++;
            Endpoint = endpoint;
            Context = ctx;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeContextFactory : IContextFactory
    {
        public IEndpointContext ExecutionContext { get; set; } = null!;
        public IGlobalContext? GlobalContext { get; private set; }
        public IMessageBuffer? Payload { get; private set; }
        public ISerializer? Serializer { get; private set; }
        public ClaimsPrincipal? User { get; private set; }
        public CancellationToken Token { get; private set; }
        public int CallCount { get; private set; }

        public IEndpointContext Create(
            IGlobalContext context,
            IMessageBuffer payload,
            ISerializer serializer,
            ClaimsPrincipal? user,
            CancellationToken token)
        {
            CallCount++;
            GlobalContext = context;
            Payload = payload;
            Serializer = serializer;
            User = user;
            Token = token;
            return ExecutionContext;
        }

        public IGlobalContext CreateGlobal(HttpContext context, WebSocket socket, string connectionId, WsOptionsSnapshot options)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeEndpointAuthorizer : IEndpointAuthorizer
    {
        public EndpointAuthResult Result { get; set; } =
            new(true, null, null, Array.Empty<string>());

        public HttpContext? HttpContext { get; private set; }
        public Endpoint? Endpoint { get; private set; }
        public CancellationToken Token { get; private set; }
        public int CallCount { get; private set; }

        public Task<EndpointAuthResult> AuthorizeAsync(HttpContext context, Endpoint endpoint, CancellationToken token)
        {
            CallCount++;
            HttpContext = context;
            Endpoint = endpoint;
            Token = token;
            return Task.FromResult(Result);
        }
    }

    private sealed class TrackingScope : IServiceScope, IAsyncDisposable
    {
        private readonly IServiceScope _inner;
        public bool DisposeCalled { get; private set; }
        public bool DisposeAsyncCalled { get; private set; }

        public TrackingScope(IServiceScope inner) => _inner = inner;
        public IServiceProvider ServiceProvider => _inner.ServiceProvider;

        public void Dispose()
        {
            DisposeCalled = true;
            _inner.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCalled = true;
            if (_inner is IAsyncDisposable ad) return ad.DisposeAsync();
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, EventId EventId, string Message)> Logs { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel level,
            EventId id,
            TState state,
            Exception? ex,
            Func<TState, Exception?, string> formatter)
            => Logs.Add((level, id, formatter(state, ex)));
    }

    private sealed class TestEndpoint { }
    private sealed class TestExecutionContext : IEndpointContext
    {
        public ISerializer Serializer => throw new NotImplementedException();

        public IMessageBuffer Payload => throw new NotImplementedException();

        public CancellationToken Cancellation => throw new NotImplementedException();

        public HttpContext Context => throw new NotImplementedException();

        public WebSocket Socket => throw new NotImplementedException();

        public WsOptionsSnapshot Options => throw new NotImplementedException();

        public string ConnectionId => throw new NotImplementedException();

        public GroupManager Groups => throw new NotImplementedException();
    }

    private readonly TestEndpoint _endpointInstance = new();
    private readonly TestExecutionContext _executionContext = new();

    private readonly FakeSerializer _serializer = new();
    private readonly FakeEndpointResolver _endpointResolver = new();
    private readonly FakeContextFactory _contextFactory = new();
    private readonly RecordingLogger<DefaultMessageDispatcher> _logger = new();
    private readonly Mock<IGlobalContext> _globalContext = new();
    private readonly DefaultHttpContext _httpContext = new();
    private readonly CancellationToken _token = new CancellationTokenSource().Token;

    private readonly TrackingScope _scope;
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();

    private readonly FakeMessageBuffer _payload = new(0x01, 0x02, 0x03, 0x04, 0x05);

    public DefaultMessageDispatcherTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_endpointInstance);
        var provider = services.BuildServiceProvider();
        _scope = new TrackingScope(provider.CreateScope());

        _scopeFactory.Setup(f => f.CreateScope()).Returns(_scope);

        _globalContext.SetupGet(c => c.Context).Returns(_httpContext);
        _contextFactory.ExecutionContext = _executionContext;
    }

    private DefaultMessageDispatcher CreateSut() =>
        new(_scopeFactory.Object, _contextFactory, _endpointResolver, _logger);

    private void SetupHttpAuthorizer(FakeEndpointAuthorizer authorizer)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEndpointAuthorizer>(authorizer);
        _httpContext.RequestServices = services.BuildServiceProvider();
    }

    private static Endpoint CreateAuthEndpoint() =>
        new(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "test-auth");

    private static WsEndpointInfo BuildEndpointInfo(
        FakeEndpointInvoker invoker,
        Endpoint? authEndpoint = null) => new()
        {
            EndpointType = typeof(TestEndpoint),
            Invoker = invoker,
            AuthEndpoint = authEndpoint,
        };

    [Fact]
    public async Task DispatchMessage_WhenKeyFieldMissing_LogsAndReturnsEarly()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = false;
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.NotEmpty(_logger.Logs);
        Assert.Equal(0, _endpointResolver.CallCount);
        Assert.Equal(0, _contextFactory.CallCount);
        _scopeFactory.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task DispatchMessage_UsesWsMessageFieldsKeyAsFieldName()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = false;
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Equal("key", _serializer.LastField);
        Assert.Equal(1, _serializer.TryGetFieldCallCount);
    }

    [Fact]
    public async Task DispatchMessage_PassesFullPayloadSpanToSerializer()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = false;
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, _serializer.LastData);
    }

    [Fact]
    public async Task DispatchMessage_WhenEndpointCannotBeResolved_LogsAndReturnsEarly()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 2;
        _endpointResolver.Result = false;
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.NotEmpty(_logger.Logs);
        Assert.Equal(1, _endpointResolver.CallCount);
        Assert.Equal(0, _contextFactory.CallCount);
        _scopeFactory.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task DispatchMessage_PassesPayloadSliceFromKeyIndexToResolver()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 2; // skip first two bytes
        _endpointResolver.Result = false;
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Equal(new byte[] { 0x03, 0x04, 0x05 }, _endpointResolver.LastSequence);
    }

    [Fact]
    public async Task DispatchMessage_WhenKeyIndexIsAtStartOfPayload_PassesWholePayloadToResolver()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        _endpointResolver.Result = false;
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, _endpointResolver.LastSequence);
    }

    [Fact]
    public async Task DispatchMessage_WhenKeyIndexIsAtEndOfPayload_PassesEmptySliceToResolver()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = _payload.Length;
        _endpointResolver.Result = false;
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.NotNull(_endpointResolver.LastSequence);
        Assert.Empty(_endpointResolver.LastSequence!);
    }

    [Fact]
    public async Task DispatchMessage_WhenNoAuthEndpoint_SkipsAuthorization()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker);

        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Equal(1, invoker.CallCount);
        Assert.Null(_contextFactory.User);
    }

    [Fact]
    public async Task DispatchMessage_WhenNoAuthEndpoint_DoesNotResolveAuthorizer()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker);

        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Equal(1, invoker.CallCount);
    }

    [Fact]
    public async Task DispatchMessage_InvokesEndpointWithResolvedInstanceAndExecutionContext()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker);
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Equal(1, invoker.CallCount);
        Assert.Same(_endpointInstance, invoker.Endpoint);
        Assert.Same(_executionContext, invoker.Context);
    }

    [Fact]
    public async Task DispatchMessage_CreatesExactlyOneScope()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker);
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        _scopeFactory.Verify(f => f.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task DispatchMessage_DisposesAsyncScopeAfterInvocation()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker);
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.True(_scope.DisposeAsyncCalled || _scope.DisposeCalled);
    }

    [Fact]
    public async Task DispatchMessage_PassesCorrectArgumentsToContextFactory()
    {
        // Arrange
        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker);
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Equal(1, _contextFactory.CallCount);
        Assert.Same(_globalContext.Object, _contextFactory.GlobalContext);
        Assert.Same(_payload, _contextFactory.Payload);
        Assert.Same(_serializer, _contextFactory.Serializer);
        Assert.Null(_contextFactory.User);
        Assert.Equal(_token, _contextFactory.Token);
    }

    [Fact]
    public async Task DispatchMessage_WhenAuthEndpointPresent_InvokesAuthorizerWithCorrectArguments()
    {
        // Arrange
        var authorizer = new FakeEndpointAuthorizer
        {
            Result = new EndpointAuthResult(true, null, null, Array.Empty<string>()),
        };
        SetupHttpAuthorizer(authorizer);

        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        var authEndpoint = CreateAuthEndpoint();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker, authEndpoint);

        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Equal(1, authorizer.CallCount);
        Assert.Same(_httpContext, authorizer.HttpContext);
        Assert.Same(authEndpoint, authorizer.Endpoint);
        Assert.Equal(_token, authorizer.Token);
    }

    [Fact]
    public async Task DispatchMessage_WhenAuthFails_LogsAndDoesNotInvokeEndpoint()
    {
        // Arrange
        var authorizer = new FakeEndpointAuthorizer
        {
            Result = new EndpointAuthResult(false, null, "invalid token", new[] { "req1" }),
        };
        SetupHttpAuthorizer(authorizer);

        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker, CreateAuthEndpoint());

        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.NotEmpty(_logger.Logs);
        Assert.Equal(1, authorizer.CallCount);
        Assert.Equal(0, invoker.CallCount);
        Assert.Equal(0, _contextFactory.CallCount);
    }

    [Fact]
    public async Task DispatchMessage_WhenAuthFails_StillDisposesScope()
    {
        // Arrange
        var authorizer = new FakeEndpointAuthorizer
        {
            Result = new EndpointAuthResult(false, null, "nope", Array.Empty<string>()),
        };
        SetupHttpAuthorizer(authorizer);

        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker, CreateAuthEndpoint());

        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.True(_scope.DisposeAsyncCalled || _scope.DisposeCalled);
    }

    [Fact]
    public async Task DispatchMessage_WhenAuthSucceeds_PassesUserToContextFactoryAndInvokesEndpoint()
    {
        // Arrange
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "alice") }, "test"));

        var authorizer = new FakeEndpointAuthorizer
        {
            Result = new EndpointAuthResult(true, user, null, Array.Empty<string>()),
        };
        SetupHttpAuthorizer(authorizer);

        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker, CreateAuthEndpoint());

        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Same(user, _contextFactory.User);
        Assert.Equal(1, invoker.CallCount);
        Assert.Equal(1, _contextFactory.CallCount);
    }

    [Fact]
    public async Task DispatchMessage_WhenAuthSucceedsButUserIsNull_PassesNullToContextFactory()
    {
        // Arrange
        var authorizer = new FakeEndpointAuthorizer
        {
            Result = new EndpointAuthResult(true, null, null, Array.Empty<string>()),
        };
        SetupHttpAuthorizer(authorizer);

        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker, CreateAuthEndpoint());

        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Null(_contextFactory.User);
        Assert.Equal(1, invoker.CallCount);
    }

    [Fact]
    public async Task DispatchMessage_WhenAuthFails_UsesFallbackFailureReason()
    {
        // Arrange
        var authorizer = new FakeEndpointAuthorizer
        {
            Result = new EndpointAuthResult(false, null, null, Array.Empty<string>()),
        };
        SetupHttpAuthorizer(authorizer);

        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker, CreateAuthEndpoint());

        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.NotEmpty(_logger.Logs);
        Assert.Contains(_logger.Logs, l => l.Message.Contains("authorization failed"));
    }

    [Fact]
    public async Task DispatchMessage_PassesCancellationTokenToContextFactory()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker);
        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, token);

        // Assert
        Assert.Equal(token, _contextFactory.Token);
    }

    [Fact]
    public async Task DispatchMessage_PassesCancellationTokenToAuthorizer()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var authorizer = new FakeEndpointAuthorizer
        {
            Result = new EndpointAuthResult(true, null, null, Array.Empty<string>()),
        };
        SetupHttpAuthorizer(authorizer);

        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker, CreateAuthEndpoint());

        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, token);

        // Assert
        Assert.Equal(token, authorizer.Token);
    }

    [Fact]
    public async Task DispatchMessage_CalledTwice_CreatesTwoIndependentScopes()
    {
        // Arrange
        var scopesCreated = new List<TrackingScope>();
        var provider = new ServiceCollection()
            .AddSingleton(_endpointInstance)
            .BuildServiceProvider();

        _scopeFactory
            .Setup(f => f.CreateScope())
            .Returns(() =>
            {
                var scope = new TrackingScope(provider.CreateScope());
                scopesCreated.Add(scope);
                return scope;
            });

        _serializer.TryGetFieldValueIndexResult = true;
        _serializer.FieldIndex = 0;
        var invoker = new FakeEndpointInvoker();
        _endpointResolver.Result = true;
        _endpointResolver.Value = BuildEndpointInfo(invoker);

        var sut = CreateSut();

        // Act
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);
        await sut.DispatchMessage(_globalContext.Object, _serializer, _payload, _token);

        // Assert
        Assert.Equal(2, scopesCreated.Count);
        Assert.All(scopesCreated, s => Assert.True(s.DisposeAsyncCalled || s.DisposeCalled));
        Assert.Equal(2, invoker.CallCount);
    }
}
