using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Buffers;
using System.Net.WebSockets;
using System.Security.Claims;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Models;
using WebSockets.Otp.Core.Services;

namespace WebSockets.Otp.Core.Tests;

public class DefaultMessageDispatcherTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IContextFactory> _contextFactoryMock = new();
    private readonly FakeTrieResolver<WsEndpointInfo> _endpointTypeResolver = new();
    private readonly Mock<ILogger<DefaultMessageDispatcher>> _loggerMock = new();
    private readonly DefaultMessageDispatcher _dispatcher;

    public DefaultMessageDispatcherTests()
    {
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _dispatcher = new DefaultMessageDispatcher(
            _scopeFactoryMock.Object,
            _contextFactoryMock.Object,
            _endpointTypeResolver,
            _loggerMock.Object);
    }

    [Fact]
    public async Task DispatchMessage_WhenKeyFieldMissing_LogsAndReturns()
    {
        // Arrange
        var contextMock = new Mock<IGlobalContext>();
        var serializer = new FakeSerializer { TryGetFieldValueIndexResult = false };
        var payload = new FakeMessageBuffer([1, 2, 3]);
        var token = CancellationToken.None;

        // Act
        await _dispatcher.DispatchMessage(contextMock.Object, serializer, payload, token);

        // Assert
        VerifyAnyLog(Times.Once());
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Never());
        Assert.Null(_endpointTypeResolver.LastInput);
        _contextFactoryMock.Verify(x => x.Create(
            It.IsAny<IGlobalContext>(),
            It.IsAny<IMessageBuffer>(),
            It.IsAny<ISerializer>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task DispatchMessage_WhenEndpointCannotBeResolved_LogsAndReturns()
    {
        // Arrange
        var contextMock = new Mock<IGlobalContext>();
        var serializer = new FakeSerializer { TryGetFieldValueIndexResult = true, KeyIndex = 2 };
        var payload = new FakeMessageBuffer([0, 1, 2, 3, 4]);
        _endpointTypeResolver.TryResolveResult = false;
        var token = CancellationToken.None;

        // Act
        await _dispatcher.DispatchMessage(contextMock.Object, serializer, payload, token);

        // Assert
        VerifyAnyLog(Times.Once());
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Never());
        Assert.NotNull(_endpointTypeResolver.LastInput);
        Assert.Equal(new byte[] { 2, 3, 4 }, _endpointTypeResolver.LastInput);
        _contextFactoryMock.Verify(x => x.Create(
            It.IsAny<IGlobalContext>(),
            It.IsAny<IMessageBuffer>(),
            It.IsAny<ISerializer>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task DispatchMessage_WhenAuthEndpointIsNull_InvokesEndpoint()
    {
        // Arrange
        var token = CancellationToken.None;
        var contextMock = new Mock<IGlobalContext>();
        var serializer = new FakeSerializer { TryGetFieldValueIndexResult = true, KeyIndex = 0 };
        var payload = new FakeMessageBuffer([1, 2, 3]);

        var endpointType = typeof(TestEndpoint);
        var endpointInstance = new TestEndpoint();
        var invokerMock = new Mock<IEndpointInvoker>();
        var endpointInfo = new WsEndpointInfo
        {
            EndpointType = endpointType,
            Invoker = invokerMock.Object,
            AuthEndpoint = null
        };
        _endpointTypeResolver.TryResolveResult = true;
        _endpointTypeResolver.Value = endpointInfo;

        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        serviceProviderMock.Setup(x => x.GetService(endpointType)).Returns(endpointInstance);

        var execCtxMock = new Mock<IEndpointContext>();
        _contextFactoryMock
            .Setup(x => x.Create(contextMock.Object, payload, serializer, token))
            .Returns(execCtxMock.Object);

        // Act
        await _dispatcher.DispatchMessage(contextMock.Object, serializer, payload, token);

        // Assert
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Once());
        serviceProviderMock.Verify(x => x.GetService(endpointType), Times.Once());
        _contextFactoryMock.Verify(x => x.Create(contextMock.Object, payload, serializer, token), Times.Once());
        invokerMock.Verify(x => x.Invoke(endpointInstance, execCtxMock.Object), Times.Once());
        scopeMock.Verify(x => x.Dispose(), Times.Once());
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DispatchMessage_WhenAuthSucceeds_InvokesEndpoint()
    {
        // Arrange
        var token = CancellationToken.None;
        var sourceHttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
            Items = new Dictionary<object, object> { ["key"] = "value" }
        };

        var contextMock = new Mock<IGlobalContext>();
        contextMock.SetupGet(x => x.Context).Returns(sourceHttpContext);

        HttpContext? capturedAuthContext = null;
        var optionsMock = new WsOptionsSnapshot(new WsOptions())
        {
            AuthPipeline = ctx =>
            {
                capturedAuthContext = ctx;
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            }
        };

        contextMock.SetupGet(x => x.Options).Returns(optionsMock);

        var serializer = new FakeSerializer { TryGetFieldValueIndexResult = true, KeyIndex = 0 };
        var payload = new FakeMessageBuffer([1, 2, 3]);

        var authEndpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "auth");
        var endpointType = typeof(TestEndpoint);
        var endpointInstance = new TestEndpoint();
        var invokerMock = new Mock<IEndpointInvoker>();
        var endpointInfo = new WsEndpointInfo
        {
            EndpointType = endpointType,
            Invoker = invokerMock.Object,
            AuthEndpoint = authEndpoint
        };
        _endpointTypeResolver.TryResolveResult = true;
        _endpointTypeResolver.Value = endpointInfo;

        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        serviceProviderMock.Setup(x => x.GetService(endpointType)).Returns(endpointInstance);

        var execCtxMock = new Mock<IEndpointContext>();
        _contextFactoryMock
            .Setup(x => x.Create(contextMock.Object, payload, serializer, token))
            .Returns(execCtxMock.Object);

        // Act
        await _dispatcher.DispatchMessage(contextMock.Object, serializer, payload, token);

        // Assert
        Assert.NotNull(capturedAuthContext);
        Assert.Same(sourceHttpContext.User, capturedAuthContext.User);
        Assert.Same(sourceHttpContext.Items, capturedAuthContext.Items);
        Assert.Equal(scopeMock.Object.ServiceProvider, capturedAuthContext.RequestServices);
        Assert.Equal(token, capturedAuthContext.RequestAborted);
        Assert.Same(authEndpoint, capturedAuthContext.GetEndpoint());

        invokerMock.Verify(x => x.Invoke(endpointInstance, execCtxMock.Object), Times.Once());
        scopeMock.Verify(x => x.Dispose(), Times.Once());
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DispatchMessage_WhenAuthReturns401_LogsAndReturnsWithoutInvoking()
    {
        // Arrange
        var token = CancellationToken.None;
        var contextMock = new Mock<IGlobalContext>();
        HttpContext? capturedAuthContext = null;
        var optionsMock = new WsOptionsSnapshot(new WsOptions())
        {
            AuthPipeline = ctx =>
            {
                capturedAuthContext = ctx;
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
        };
        contextMock.SetupGet(x => x.Options).Returns(optionsMock);
        contextMock.SetupGet(x => x.Context).Returns(new DefaultHttpContext());

        var serializer = new FakeSerializer { TryGetFieldValueIndexResult = true, KeyIndex = 0 };
        var payload = new FakeMessageBuffer([1, 2, 3]);

        var authEndpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "auth");
        var endpointType = typeof(TestEndpoint);
        var invokerMock = new Mock<IEndpointInvoker>();
        var endpointInfo = new WsEndpointInfo
        {
            EndpointType = endpointType,
            Invoker = invokerMock.Object,
            AuthEndpoint = authEndpoint
        };
        _endpointTypeResolver.TryResolveResult = true;
        _endpointTypeResolver.Value = endpointInfo;

        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        // Act
        await _dispatcher.DispatchMessage(contextMock.Object, serializer, payload, token);

        // Assert
        VerifyAnyLog(Times.Once());
        invokerMock.Verify(x => x.Invoke(It.IsAny<object>(), It.IsAny<IEndpointContext>()), Times.Never());
        _contextFactoryMock.Verify(x => x.Create(
            It.IsAny<IGlobalContext>(),
            It.IsAny<IMessageBuffer>(),
            It.IsAny<ISerializer>(),
            It.IsAny<CancellationToken>()), Times.Never());
        serviceProviderMock.Verify(x => x.GetService(It.IsAny<Type>()), Times.Never());
        scopeMock.Verify(x => x.Dispose(), Times.Once());
    }

    [Fact]
    public async Task DispatchMessage_WhenAuthReturns403_LogsAndReturnsWithoutInvoking()
    {
        // Arrange
        var token = CancellationToken.None;
        var contextMock = new Mock<IGlobalContext>();
        HttpContext? capturedAuthContext = null;
        var optionsMock = new WsOptionsSnapshot(new WsOptions())
        {
            AuthPipeline = ctx =>
            {
                capturedAuthContext = ctx;
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
        contextMock.SetupGet(x => x.Options).Returns(optionsMock);
        contextMock.SetupGet(x => x.Context).Returns(new DefaultHttpContext());

        var serializer = new FakeSerializer { TryGetFieldValueIndexResult = true, KeyIndex = 0 };
        var payload = new FakeMessageBuffer([1, 2, 3]);

        var authEndpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "auth");
        var endpointType = typeof(TestEndpoint);
        var invokerMock = new Mock<IEndpointInvoker>();
        var endpointInfo = new WsEndpointInfo
        {
            EndpointType = endpointType,
            Invoker = invokerMock.Object,
            AuthEndpoint = authEndpoint
        };
        _endpointTypeResolver.TryResolveResult = true;
        _endpointTypeResolver.Value = endpointInfo;

        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        // Act
        await _dispatcher.DispatchMessage(contextMock.Object, serializer, payload, token);

        // Assert
        VerifyAnyLog(Times.Once());
        invokerMock.Verify(x => x.Invoke(It.IsAny<object>(), It.IsAny<IEndpointContext>()), Times.Never());
        _contextFactoryMock.Verify(x => x.Create(
            It.IsAny<IGlobalContext>(),
            It.IsAny<IMessageBuffer>(),
            It.IsAny<ISerializer>(),
            It.IsAny<CancellationToken>()), Times.Never());
        serviceProviderMock.Verify(x => x.GetService(It.IsAny<Type>()), Times.Never());
        scopeMock.Verify(x => x.Dispose(), Times.Once());
    }

    [Fact]
    public async Task DispatchMessage_PassesCancellationTokenToContextFactory()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var contextMock = new Mock<IGlobalContext>();
        var serializer = new FakeSerializer { TryGetFieldValueIndexResult = true, KeyIndex = 0 };
        var payload = new FakeMessageBuffer([1, 2, 3]);

        var endpointType = typeof(TestEndpoint);
        var endpointInstance = new TestEndpoint();
        var invokerMock = new Mock<IEndpointInvoker>();
        var endpointInfo = new WsEndpointInfo
        {
            EndpointType = endpointType,
            Invoker = invokerMock.Object,
            AuthEndpoint = null
        };
        _endpointTypeResolver.TryResolveResult = true;
        _endpointTypeResolver.Value = endpointInfo;

        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        serviceProviderMock.Setup(x => x.GetService(endpointType)).Returns(endpointInstance);

        var execCtxMock = new Mock<IEndpointContext>();
        _contextFactoryMock
            .Setup(x => x.Create(contextMock.Object, payload, serializer, token))
            .Returns(execCtxMock.Object);

        // Act
        await _dispatcher.DispatchMessage(contextMock.Object, serializer, payload, token);

        // Assert
        _contextFactoryMock.Verify(x => x.Create(contextMock.Object, payload, serializer, token), Times.Once());
    }

    private void VerifyAnyLog(Times times)
    {
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    private sealed class FakeMessageBuffer : IMessageBuffer
    {
        private readonly byte[] _buffer;
        public FakeMessageBuffer(byte[] data)
        {
            _buffer = data;
            Length = data.Length;
            Capacity = data.Length;
        }

        public int Length { get; private set; }
        public int Capacity { get; private set; }
        public Span<byte> Span => _buffer.AsSpan(0, Length);
        public Memory<byte> Memory => _buffer.AsMemory(0, Length);

        public void Write(ReadOnlySpan<byte> data) => throw new NotSupportedException();
        public void Write(ReadOnlySequence<byte> data) => throw new NotSupportedException();
        public void SetLength(int length) => Length = length;
        public void Shrink() { }
        public void Dispose() { }
    }

    private sealed class FakeSerializer : ISerializer
    {
        public bool TryGetFieldValueIndexResult { get; set; }
        public int KeyIndex { get; set; }

        public string Protocol => "test";
        public WebSocketMessageType Type => WebSocketMessageType.Text;

        public ReadOnlyMemory<byte> Serialize<T>(T message) => throw new NotSupportedException();
        public T? Deserialize<T>(ReadOnlySpan<byte> data) => throw new NotSupportedException();

        public bool TryGetFieldValueIndex(ReadOnlySpan<byte> data, string field, out int index)
        {
            index = KeyIndex;
            return TryGetFieldValueIndexResult;
        }
    }

    private sealed class FakeTrieResolver<T> : ITrieResolver<T>
    {
        public bool TryResolveResult { get; set; }
        public T? Value { get; set; }
        public byte[]? LastInput { get; private set; }

        public bool TryResolve(ReadOnlySpan<byte> sequence, out T? value)
        {
            LastInput = sequence.ToArray();
            value = Value;
            return TryResolveResult;
        }
    }

    private sealed class TestEndpoint { }
}
