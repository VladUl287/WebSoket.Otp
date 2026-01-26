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
}
