using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Services;


namespace WebSockets.Otp.Core.Tests.Services;

public class DefaultConnectionHandlerTests
{
    private readonly Mock<IWsConnectionManager> _connectionManagerMock;
    private readonly Mock<IWsConnectionFactory> _connectionFactoryMock;
    private readonly Mock<IHandshakeHandler> _handshakeServiceMock;
    private readonly Mock<IContextFactory> _contextFactoryMock;
    private readonly Mock<IMessageProcessorStore> _processorResolverMock;
    private readonly Mock<ISerializerStore> _serializerStoreMock;
    private readonly DefaultConnectionHandler _handler;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<WebSocket> _webSocketMock;

    public DefaultConnectionHandlerTests()
    {
        _connectionManagerMock = new Mock<IWsConnectionManager>();
        _connectionFactoryMock = new Mock<IWsConnectionFactory>();
        _handshakeServiceMock = new Mock<IHandshakeHandler>();
        _contextFactoryMock = new Mock<IContextFactory>();
        _processorResolverMock = new Mock<IMessageProcessorStore>();
        _serializerStoreMock = new Mock<ISerializerStore>();

        _handler = new DefaultConnectionHandler(
            _connectionManagerMock.Object,
            _connectionFactoryMock.Object,
            _handshakeServiceMock.Object,
            _contextFactoryMock.Object,
            _processorResolverMock.Object,
            _serializerStoreMock.Object,
            NullLogger<DefaultConnectionHandler>.Instance
        );

        _httpContextMock = new Mock<HttpContext>();
        _webSocketMock = new Mock<WebSocket>();
    }

    [Fact]
    public async Task HandleAsync_WhenWebSocketAccepted_InvokesAcceptWebSocketAsync()
    {
        // Arrange
        var webSocketManagerMock = new Mock<WebSocketManager>();
        var config = new WsConfiguration(new WsOptions());
        var token = new CancellationTokenSource().Token;

        _httpContextMock.Setup(x => x.WebSockets).Returns(webSocketManagerMock.Object);
        _httpContextMock.Setup(x => x.RequestAborted).Returns(token);

        webSocketManagerMock.Setup(x => x.AcceptWebSocketAsync())
            .ReturnsAsync(_webSocketMock.Object);

        _handshakeServiceMock.Setup(x => x.HandleAsync(It.IsAny<HttpContext>(), It.IsAny<WebSocket>(), It.IsAny<WsConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WsHandshakeOptions?)null);

        // Act
        await _handler.HandleAsync(_httpContextMock.Object, config);

        // Assert
        webSocketManagerMock.Verify(x => x.AcceptWebSocketAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenHandshakeReturnsNull_ReturnsEarly()
    {
        // Arrange
        var webSocketManagerMock = new Mock<WebSocketManager>();
        var config = new WsConfiguration(new WsOptions());
        var token = new CancellationTokenSource().Token;

        _httpContextMock.Setup(x => x.WebSockets).Returns(webSocketManagerMock.Object);
        _httpContextMock.Setup(x => x.RequestAborted).Returns(token);

        webSocketManagerMock.Setup(x => x.AcceptWebSocketAsync())
            .ReturnsAsync(_webSocketMock.Object);

        _handshakeServiceMock.Setup(x => x.HandleAsync(It.IsAny<HttpContext>(), It.IsAny<WebSocket>(), It.IsAny<WsConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WsHandshakeOptions?)null);

        // Act
        await _handler.HandleAsync(_httpContextMock.Object, config);

        // Assert
        _connectionFactoryMock.Verify(x => x.Create(It.IsAny<WebSocket>(), It.IsAny<ISerializer>()), Times.Never);
        _connectionManagerMock.Verify(x => x.TryAdd(It.IsAny<IWsConnection>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSerializerNotFound_ReturnsEarly()
    {
        // Arrange
        var webSocketManagerMock = new Mock<WebSocketManager>();
        var config = new WsConfiguration(new WsOptions());
        var token = new CancellationTokenSource().Token;
        var handshakeOptions = new WsHandshakeOptions()
        {
            Protocol = "test-protocol"
        };

        _httpContextMock.Setup(x => x.WebSockets).Returns(webSocketManagerMock.Object);
        _httpContextMock.Setup(x => x.RequestAborted).Returns(token);

        webSocketManagerMock.Setup(x => x.AcceptWebSocketAsync())
            .ReturnsAsync(_webSocketMock.Object);

        _handshakeServiceMock.Setup(x => x.HandleAsync(It.IsAny<HttpContext>(), It.IsAny<WebSocket>(), It.IsAny<WsConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handshakeOptions);

        _serializerStoreMock.Setup(x => x.TryGet("test-protocol", out It.Ref<ISerializer>.IsAny))
            .Returns(false);

        // Act
        await _handler.HandleAsync(_httpContextMock.Object, config);

        // Assert
        _connectionFactoryMock.Verify(x => x.Create(It.IsAny<WebSocket>(), It.IsAny<ISerializer>()), Times.Never);
        _connectionManagerMock.Verify(x => x.TryAdd(It.IsAny<IWsConnection>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenConnectionAddFails_ReturnsEarly()
    {
        // Arrange
        var webSocketManagerMock = new Mock<WebSocketManager>();
        var config = new WsConfiguration(new WsOptions());
        var token = new CancellationTokenSource().Token;
        var handshakeOptions = new WsHandshakeOptions()
        {
            Protocol = "test-protocol"
        };
        var serializerMock = new Mock<ISerializer>();
        var connectionMock = new Mock<IWsConnection>();

        _httpContextMock.Setup(x => x.WebSockets).Returns(webSocketManagerMock.Object);
        _httpContextMock.Setup(x => x.RequestAborted).Returns(token);

        webSocketManagerMock.Setup(x => x.AcceptWebSocketAsync())
            .ReturnsAsync(_webSocketMock.Object);

        _handshakeServiceMock.Setup(x => x.HandleAsync(It.IsAny<HttpContext>(), It.IsAny<WebSocket>(), It.IsAny<WsConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handshakeOptions);

        _serializerStoreMock.Setup(x => x.TryGet("test-protocol", out It.Ref<ISerializer>.IsAny))
            .Returns(true)
            .Callback((string protocol, out ISerializer serializer) => serializer = serializerMock.Object);

        _connectionFactoryMock.Setup(x => x.Create(_webSocketMock.Object, serializerMock.Object))
            .Returns(connectionMock.Object);

        _connectionManagerMock.Setup(x => x.TryAdd(connectionMock.Object, token))
            .ReturnsAsync(false);

        // Act
        await _handler.HandleAsync(_httpContextMock.Object, config);

        // Assert
        _contextFactoryMock.Verify(x => x.CreateGlobal(It.IsAny<HttpContext>(), It.IsAny<WebSocket>(), It.IsAny<string>(), It.IsAny<IWsConnectionManager>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenConnectionEstablished_InvokesOnConnectedCallback()
    {
        // Arrange
        var webSocketManagerMock = new Mock<WebSocketManager>();
        var onConnectedInvoked = false;
        var config = new WsConfiguration(new WsOptions()
        {
            OnConnected = (context) => {
                onConnectedInvoked = true;
                return Task.CompletedTask;
            }
        });
        var token = new CancellationTokenSource().Token;
        var handshakeOptions = new WsHandshakeOptions()
        {
            Protocol = "test-protocol"
        };
        var serializerMock = new Mock<ISerializer>();
        var connectionMock = new Mock<IWsConnection>();
        var messageProcessorMock = new Mock<IMessageProcessor>();
        var globalContextMock = new Mock<IGlobalContext>();
        var connectionId = Guid.NewGuid().ToString();

        _httpContextMock.Setup(x => x.WebSockets).Returns(webSocketManagerMock.Object);
        _httpContextMock.Setup(x => x.RequestAborted).Returns(token);

        webSocketManagerMock.Setup(x => x.AcceptWebSocketAsync())
            .ReturnsAsync(_webSocketMock.Object);

        _handshakeServiceMock.Setup(x => x.HandleAsync(It.IsAny<HttpContext>(), It.IsAny<WebSocket>(), It.IsAny<WsConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handshakeOptions);

        _serializerStoreMock.Setup(x => x.TryGet("test-protocol", out It.Ref<ISerializer>.IsAny))
            .Returns(true)
            .Callback((string protocol, out ISerializer serializer) => serializer = serializerMock.Object);

        connectionMock.SetupGet(x => x.Id).Returns(connectionId);
        _connectionFactoryMock.Setup(x => x.Create(_webSocketMock.Object, serializerMock.Object))
            .Returns(connectionMock.Object);

        _connectionManagerMock.Setup(x => x.TryAdd(connectionMock.Object, token))
            .ReturnsAsync(true);

        _contextFactoryMock.Setup(x => x.CreateGlobal(_httpContextMock.Object, _webSocketMock.Object, connectionId, _connectionManagerMock.Object))
            .Returns(globalContextMock.Object);

        _processorResolverMock.Setup(x => x.Get(It.IsAny<ProcessingMode>()))
            .Returns(messageProcessorMock.Object);

        messageProcessorMock.Setup(x => x.Process(globalContextMock.Object, serializerMock.Object, config, token))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(_httpContextMock.Object, config);

        // Assert
        Assert.True(onConnectedInvoked);
        _processorResolverMock.Verify(x => x.Get(config.ProcessingMode), Times.Once);
        messageProcessorMock.Verify(x => x.Process(globalContextMock.Object, serializerMock.Object, config, token), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenMessageProcessorThrows_StillCleansUpConnection()
    {
        // Arrange
        var webSocketManagerMock = new Mock<WebSocketManager>();
        var onDisconnectedInvoked = false;
        var config = new WsConfiguration(new WsOptions()
        {
            OnDisconnected = (context) => {
                onDisconnectedInvoked = true;
                return Task.CompletedTask;
            }
        });
        var token = new CancellationTokenSource().Token;
        var handshakeOptions = new WsHandshakeOptions()
        {
            Protocol = "test-protocol"
        };
        var serializerMock = new Mock<ISerializer>();
        var connectionMock = new Mock<IWsConnection>();
        var messageProcessorMock = new Mock<IMessageProcessor>();
        var globalContextMock = new Mock<IGlobalContext>();
        var connectionId = Guid.NewGuid().ToString();

        _httpContextMock.Setup(x => x.WebSockets).Returns(webSocketManagerMock.Object);
        _httpContextMock.Setup(x => x.RequestAborted).Returns(token);

        webSocketManagerMock.Setup(x => x.AcceptWebSocketAsync())
            .ReturnsAsync(_webSocketMock.Object);

        _handshakeServiceMock.Setup(x => x.HandleAsync(It.IsAny<HttpContext>(), It.IsAny<WebSocket>(), It.IsAny<WsConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handshakeOptions);

        _serializerStoreMock.Setup(x => x.TryGet("test-protocol", out It.Ref<ISerializer>.IsAny))
            .Returns(true)
            .Callback((string protocol, out ISerializer serializer) => serializer = serializerMock.Object);

        connectionMock.SetupGet(x => x.Id).Returns(connectionId);
        _connectionFactoryMock.Setup(x => x.Create(_webSocketMock.Object, serializerMock.Object))
            .Returns(connectionMock.Object);

        _connectionManagerMock.Setup(x => x.TryAdd(connectionMock.Object, token))
            .ReturnsAsync(true);

        _contextFactoryMock.Setup(x => x.CreateGlobal(_httpContextMock.Object, _webSocketMock.Object, connectionId, _connectionManagerMock.Object))
            .Returns(globalContextMock.Object);

        _processorResolverMock.Setup(x => x.Get(It.IsAny<ProcessingMode>()))
            .Returns(messageProcessorMock.Object);

        messageProcessorMock.Setup(x => x.Process(globalContextMock.Object, serializerMock.Object, config, token))
            .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _handler.HandleAsync(_httpContextMock.Object, config));

        // Verify cleanup still happened
        _connectionManagerMock.Verify(x => x.TryRemove(connectionId, token), Times.Once);
        Assert.True(onDisconnectedInvoked);
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessful_CompletesFullLifecycle()
    {
        // Arrange
        var webSocketManagerMock = new Mock<WebSocketManager>();
        var onConnectedInvoked = false;
        var onDisconnectedInvoked = false;
        var config = new WsConfiguration(new WsOptions()
        {
            OnConnected = (context) => {
                onConnectedInvoked = true;
                return Task.CompletedTask;
            },
            OnDisconnected = (context) => {
                onDisconnectedInvoked = true;
                return Task.CompletedTask;
            }
        });
        var token = new CancellationTokenSource().Token;
        var handshakeOptions = new WsHandshakeOptions()
        {
            Protocol = "test-protocol"
        };
        var serializerMock = new Mock<ISerializer>();
        var connectionMock = new Mock<IWsConnection>();
        var messageProcessorMock = new Mock<IMessageProcessor>();
        var globalContextMock = new Mock<IGlobalContext>();
        var connectionId = Guid.NewGuid().ToString();

        _httpContextMock.Setup(x => x.WebSockets).Returns(webSocketManagerMock.Object);
        _httpContextMock.Setup(x => x.RequestAborted).Returns(token);

        webSocketManagerMock.Setup(x => x.AcceptWebSocketAsync())
            .ReturnsAsync(_webSocketMock.Object);

        _handshakeServiceMock.Setup(x => x.HandleAsync(It.IsAny<HttpContext>(), It.IsAny<WebSocket>(), It.IsAny<WsConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handshakeOptions);

        _serializerStoreMock.Setup(x => x.TryGet("test-protocol", out It.Ref<ISerializer>.IsAny))
            .Returns(true)
            .Callback((string protocol, out ISerializer serializer) => serializer = serializerMock.Object);

        connectionMock.SetupGet(x => x.Id).Returns(connectionId);
        _connectionFactoryMock.Setup(x => x.Create(_webSocketMock.Object, serializerMock.Object))
            .Returns(connectionMock.Object);

        _connectionManagerMock.Setup(x => x.TryAdd(connectionMock.Object, token))
            .ReturnsAsync(true);

        _contextFactoryMock.Setup(x => x.CreateGlobal(_httpContextMock.Object, _webSocketMock.Object, connectionId, _connectionManagerMock.Object))
            .Returns(globalContextMock.Object);

        _processorResolverMock.Setup(x => x.Get(It.IsAny<ProcessingMode>()))
            .Returns(messageProcessorMock.Object);

        messageProcessorMock.Setup(x => x.Process(globalContextMock.Object, serializerMock.Object, config, token))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(_httpContextMock.Object, config);

        // Assert
        Assert.True(onConnectedInvoked);
        Assert.True(onDisconnectedInvoked);
        _connectionManagerMock.Verify(x => x.TryRemove(connectionId, token), Times.Once);
        _connectionManagerMock.Verify(x => x.TryAdd(connectionMock.Object, token), Times.Once);
        messageProcessorMock.Verify(x => x.Process(globalContextMock.Object, serializerMock.Object, config, token), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenWebSocketDisposed_DoesNotThrow()
    {
        // Arrange
        var webSocketManagerMock = new Mock<WebSocketManager>();
        var config = new WsConfiguration(new WsOptions());
        var token = new CancellationTokenSource().Token;

        _httpContextMock.Setup(x => x.WebSockets).Returns(webSocketManagerMock.Object);
        _httpContextMock.Setup(x => x.RequestAborted).Returns(token);

        webSocketManagerMock.Setup(x => x.AcceptWebSocketAsync())
            .ReturnsAsync(_webSocketMock.Object);

        _webSocketMock.Setup(x => x.Dispose()).Throws(new ObjectDisposedException("WebSocket"));

        _handshakeServiceMock.Setup(x => x.HandleAsync(It.IsAny<HttpContext>(), It.IsAny<WebSocket>(), It.IsAny<WsConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WsHandshakeOptions?)null);

        // Act & Assert
        await _handler.HandleAsync(_httpContextMock.Object, config);

        // Should not throw
        Assert.True(true);
    }
}
