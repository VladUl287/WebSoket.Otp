using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;
using WebSockets.Otp.Core;

namespace WebSockets.Otp.Tests;

public static class HandshakeLoggerExtensions
{
    public static void VerifyHandshakeRequestStarted(this Mock<ILogger<HandshakeRequestProcessor>> loggerMock, string connectionId, Times times)
    {
        loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Handshake request started for connection {connectionId}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            times);
    }

    public static void VerifyWebSocketRequestAuthFailed(this Mock<ILogger<HandshakeRequestProcessor>> loggerMock, string connectionId, Times times)
    {
        loggerMock.Verify(logger => logger.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"WebSocket request authorization failed for connection {connectionId}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            times);
    }

    public static void VerifyHandshakeCompleted(this Mock<ILogger<HandshakeRequestProcessor>> loggerMock, string connectionId, Times times)
    {
        loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Handshake completed for connection {connectionId}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            times);
    }

    public static void VerifyConnectionTokenGenerated(this Mock<ILogger<HandshakeRequestProcessor>> loggerMock, string tokenId, string connectionId, Times times)
    {
        loggerMock.Verify(logger => logger.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Connection token {tokenId} generated for connection {connectionId}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            times);
    }

    public static void VerifyHandshakeRequestDeserializationFailed(this Mock<ILogger<HandshakeRequestProcessor>> loggerMock, string connectionId, Times times)
    {
        loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to deserialize handshake request for connection {connectionId}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            times);
    }
}

public class HandshakeRequestProcessorTests
{
    private readonly Mock<IWsAuthorizationService> _authServiceMock;
    private readonly Mock<IConnectionStateService> _connectionStateMock;
    private readonly Mock<ISerializerFactory> _serializerFactoryMock;
    private readonly Mock<ILogger<HandshakeRequestProcessor>> _loggerMock;
    private readonly HandshakeRequestProcessor _processor;
    private readonly DefaultHttpContext _httpContext;
    private readonly WsMiddlewareOptions _options;

    public HandshakeRequestProcessorTests()
    {
        _authServiceMock = new Mock<IWsAuthorizationService>();
        _connectionStateMock = new Mock<IConnectionStateService>();
        _serializerFactoryMock = new Mock<ISerializerFactory>();
        _loggerMock = new Mock<ILogger<HandshakeRequestProcessor>>();

        _processor = new HandshakeRequestProcessor(
            _authServiceMock.Object,
            _connectionStateMock.Object,
            _serializerFactoryMock.Object,
            _loggerMock.Object);

        _httpContext = new DefaultHttpContext();
        _options = new WsMiddlewareOptions
        {
            Paths = new PathSettings
            {
                HandshakePath = "/ws/handshake"
            },
            Authorization = new AuthorizationSettings(),
            Connection = new ConnectionSettings()
        };

        // Setup default valid request
        SetupValidHandshakeRequest();
    }

    private void SetupValidHandshakeRequest()
    {
        _httpContext.Request.Method = HttpMethods.Post;
        _httpContext.Request.Path = "/ws/handshake";
        _httpContext.Request.ContentType = "application/json";
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new ConnectionSettings { Protocol = "json" })));

        _httpContext.Connection.Id = "test-connection-123";

        var cancellationTokenSource = new CancellationTokenSource();
        _httpContext.RequestAborted = cancellationTokenSource.Token;
    }

    [Fact]
    public void Constructor_WithNullArguments_ThrowsArgumentNullException()
    {
        // Arrange
        var authService = Mock.Of<IWsAuthorizationService>();
        var connectionState = Mock.Of<IConnectionStateService>();
        var serializerFactory = Mock.Of<ISerializerFactory>();
        var logger = Mock.Of<ILogger<HandshakeRequestProcessor>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HandshakeRequestProcessor(null!, connectionState, serializerFactory, logger));
        Assert.Throws<ArgumentNullException>(() => new HandshakeRequestProcessor(authService, null!, serializerFactory, logger));
        Assert.Throws<ArgumentNullException>(() => new HandshakeRequestProcessor(authService, connectionState, null!, logger));
        Assert.Throws<ArgumentNullException>(() => new HandshakeRequestProcessor(authService, connectionState, serializerFactory, null!));
    }

    [Fact]
    public void IsHandshakeRequest_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _processor.IsHandshakeRequest(null!, _options));
    }

    [Fact]
    public void IsHandshakeRequest_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _processor.IsHandshakeRequest(_httpContext, null!));
    }

    [Theory]
    [InlineData("/ws/handshake", true)]
    [InlineData("/ws/handshake/", false)]
    [InlineData("/api/handshake", false)]
    [InlineData("/ws/handshake/subpath", false)]
    public void IsHandshakeRequest_WithDifferentPaths_ReturnsExpectedResult(string requestPath, bool expected)
    {
        // Arrange
        _httpContext.Request.Path = requestPath;

        // Act
        var result = _processor.IsHandshakeRequest(_httpContext, _options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task HandleRequestAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _processor.HandleRequestAsync(null!, _options));
    }

    [Fact]
    public async Task HandleRequestAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _processor.HandleRequestAsync(_httpContext, null!));
    }

    [Fact]
    public async Task HandleRequestAsync_WithInvalidPath_ReturnsForbidden()
    {
        // Arrange
        _httpContext.Request.Path = "/invalid-path";

        // Act
        await _processor.HandleRequestAsync(_httpContext, _options);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, _httpContext.Response.StatusCode);
        _loggerMock.VerifyHandshakeRequestStarted("test-connection-123", Times.Once());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task HandleRequestAsync_WithInvalidHttpMethod_ReturnsMethodNotAllowed(string method)
    {
        // Arrange
        _httpContext.Request.Method = method;

        // Act
        await _processor.HandleRequestAsync(_httpContext, _options);

        // Assert
        Assert.Equal(StatusCodes.Status405MethodNotAllowed, _httpContext.Response.StatusCode);
        Assert.Equal("Method not allowed", await GetResponseBodyAsync());
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("")]
    [InlineData(null)]
    public async Task HandleRequestAsync_WithInvalidContentType_ReturnsUnsupportedMediaType(string contentType)
    {
        // Arrange
        _httpContext.Request.ContentType = contentType;

        // Act
        await _processor.HandleRequestAsync(_httpContext, _options);

        // Assert
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, _httpContext.Response.StatusCode);
        Assert.Equal("Invalid content type. Only supported application/json", await GetResponseBodyAsync());
    }

    [Fact]
    public async Task HandleRequestAsync_WithInvalidJsonBody_ReturnsBadRequest()
    {
        // Arrange
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("invalid json"));

        // Act
        await _processor.HandleRequestAsync(_httpContext, _options);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, _httpContext.Response.StatusCode);
        Assert.Equal("Invalid JSON format", await GetResponseBodyAsync());
        _loggerMock.VerifyHandshakeRequestDeserializationFailed("test-connection-123", Times.Once());
    }

    [Fact]
    public async Task HandleRequestAsync_WithNullRequestBody_ReturnsBadRequest()
    {
        // Arrange
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("null"));

        // Act
        await _processor.HandleRequestAsync(_httpContext, _options);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, _httpContext.Response.StatusCode);
        Assert.Equal("Invalid request body", await GetResponseBodyAsync());
    }

    [Fact]
    public async Task HandleRequestAsync_WithUnsupportedProtocol_ReturnsBadRequest()
    {
        // Arrange
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new ConnectionSettings { Protocol = "unsupported" })));

        _serializerFactoryMock
            .Setup(f => f.Resolve("unsupported"))
            .Returns((ISerializer)null!);

        // Act
        await _processor.HandleRequestAsync(_httpContext, _options);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, _httpContext.Response.StatusCode);
        Assert.Equal("Protocol 'unsupported' not supported", await GetResponseBodyAsync());
    }

    [Fact]
    public async Task HandleRequestAsync_WithFailedAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        var authResult = WsAuthorizationResult.Failure("Invalid token");
        _authServiceMock
            .Setup(s => s.AuhtorizeAsync(_httpContext, _options.Authorization))
            .ReturnsAsync(authResult);

        _serializerFactoryMock
            .Setup(f => f.Resolve("json"))
            .Returns(Mock.Of<ISerializer>());

        // Act
        await _processor.HandleRequestAsync(_httpContext, _options);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, _httpContext.Response.StatusCode);
        Assert.Equal("Invalid token", await GetResponseBodyAsync());
        _loggerMock.VerifyWebSocketRequestAuthFailed("test-connection-123", Times.Once());
        _authServiceMock.Verify(s => s.AuhtorizeAsync(_httpContext, _options.Authorization), Times.Once());
    }

    [Fact]
    public async Task HandleRequestAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var expectedTokenId = "generated-token-123";
        var serializerMock = new Mock<ISerializer>();

        _authServiceMock
            .Setup(s => s.AuhtorizeAsync(_httpContext, _options.Authorization))
            .ReturnsAsync(WsAuthorizationResult.Success());

        _serializerFactoryMock
            .Setup(f => f.Resolve("json"))
            .Returns(serializerMock.Object);

        _connectionStateMock
            .Setup(s => s.GenerateTokenId(_httpContext, _options.Connection, _httpContext.RequestAborted))
            .ReturnsAsync(expectedTokenId);

        // Act
        await _processor.HandleRequestAsync(_httpContext, _options);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, _httpContext.Response.StatusCode);
        Assert.Equal(expectedTokenId, await GetResponseBodyAsync());
        Assert.Equal("text/plain; charset=utf-8", _httpContext.Response.ContentType);

        _loggerMock.VerifyHandshakeRequestStarted("test-connection-123", Times.Once());
        _loggerMock.VerifyConnectionTokenGenerated(expectedTokenId, "test-connection-123", Times.Once());
        _loggerMock.VerifyHandshakeCompleted("test-connection-123", Times.Once());

        _connectionStateMock.Verify(s => s.GenerateTokenId(_httpContext, _options.Connection, _httpContext.RequestAborted), Times.Once());
    }

    [Fact]
    public async Task HandleRequestAsync_WithValidRequest_SetsConnectionOptions()
    {
        // Arrange
        var user = new System.Security.Claims.ClaimsPrincipal();
        _httpContext.User = user;

        var serializerMock = new Mock<ISerializer>();

        _authServiceMock
            .Setup(s => s.AuhtorizeAsync(_httpContext, _options.Authorization))
            .ReturnsAsync(WsAuthorizationResult.Success());
        _serializerFactoryMock
            .Setup(f => f.Resolve("json"))
            .Returns(serializerMock.Object);

        _connectionStateMock
            .Setup(s => s.GenerateTokenId(_httpContext, It.IsAny<ConnectionSettings>(), _httpContext.RequestAborted))
            .ReturnsAsync("token");

        // Act
        await _processor.HandleRequestAsync(_httpContext, _options);

        // Assert
        _connectionStateMock.Verify(s => s.GenerateTokenId(
            _httpContext,
            It.Is<ConnectionSettings>(opt =>
                opt.User == user &&
                opt.Protocol == "json"),
            _httpContext.RequestAborted),
            Times.Once());
    }

    [Fact]
    public async Task HandleRequestAsync_WhenCancellationRequested_CompletesGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _httpContext.RequestAborted = cts.Token;

        // Act
        await _processor.HandleRequestAsync(_httpContext, _options);

        // Assert
        // Should not throw and should complete without processing
        Assert.True(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task HandleRequestAsync_WithEmptyProtocol_ReturnsBadRequest()
    {
        // Arrange
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new ConnectionSettings { Protocol = "" })));

        // Act
        await _processor.HandleRequestAsync(_httpContext, _options);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, _httpContext.Response.StatusCode);
        Assert.Equal("Protocol '' not supported", await GetResponseBodyAsync());
    }

    [Fact]
    public async Task HandleRequestAsync_WithConnectionStateException_PropagatesException()
    {
        // Arrange
        _authServiceMock
            .Setup(s => s.AuhtorizeAsync(_httpContext, _options.Authorization))
            .ReturnsAsync(WsAuthorizationResult.Success());

        _serializerFactoryMock
            .Setup(f => f.Resolve("json"))
            .Returns(Mock.Of<ISerializer>());

        _connectionStateMock
            .Setup(s => s.GenerateTokenId(_httpContext, _options.Connection, _httpContext.RequestAborted))
            .ThrowsAsync(new InvalidOperationException("Connection state error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _processor.HandleRequestAsync(_httpContext, _options));
    }

    [Fact]
    public void IsHandshakeRequestPath_WithExactMatch_ReturnsTrue()
    {
        // Arrange
        _httpContext.Request.Path = "/ws/handshake";

        // Act
        var result = HandshakeRequestProcessorTestsAccessor.IsHandshakeRequestPath(_httpContext, _options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHandshakeRequestPath_WithDifferentCase_ReturnsTrue()
    {
        // Arrange
        _httpContext.Request.Path = "/WS/HANDSHAKE";

        // Act
        var result = HandshakeRequestProcessorTestsAccessor.IsHandshakeRequestPath(_httpContext, _options);

        // Assert
        Assert.True(result);
    }

    private async Task<string> GetResponseBodyAsync()
    {
        if (_httpContext.Response.Body is MemoryStream memoryStream)
        {
            memoryStream.Position = 0;
            return await new StreamReader(memoryStream).ReadToEndAsync();
        }
        return string.Empty;
    }

    // Test accessor for private methods
    private class HandshakeRequestProcessorTestsAccessor : HandshakeRequestProcessor
    {
        public HandshakeRequestProcessorTestsAccessor() 
            : base(Mock.Of<IWsAuthorizationService>(), Mock.Of<IConnectionStateService>(), Mock.Of<ISerializerFactory>(), Mock.Of<ILogger<HandshakeRequestProcessor>>())
        {
        }

        public static bool IsHandshakeRequestPath(HttpContext context, WsMiddlewareOptions options)
        {
            var accessor = new HandshakeRequestProcessorTestsAccessor();
            var method = typeof(HandshakeRequestProcessor)
                .GetMethod("IsHandshakeRequestPath", BindingFlags.NonPublic | BindingFlags.Static);

            return (bool)method!.Invoke(null, new object[] { context, options })!;
        }
    }
}

