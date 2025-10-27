using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core;
using Xunit;

namespace WebSockets.Otp.Tests;

public class WsRequestProcessorTests
{
    private readonly Mock<IConnectionStateService> _mockConnectionStateService;
    private readonly Mock<IWsConnectionFactory> _mockConnectionFactory;
    private readonly Mock<IWsService> _mockWsService;
    private readonly Mock<ILogger<WsRequestProcessor>> _mockLogger;
    private readonly WsRequestProcessor _processor;

    public WsRequestProcessorTests()
    {
        _mockConnectionStateService = new Mock<IConnectionStateService>();
        _mockConnectionFactory = new Mock<IWsConnectionFactory>();
        _mockWsService = new Mock<IWsService>();
        _mockLogger = new Mock<ILogger<WsRequestProcessor>>();

        _processor = new WsRequestProcessor(
            _mockConnectionStateService.Object,
            _mockConnectionFactory.Object,
            _mockWsService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new WsRequestProcessor(null, _mockConnectionFactory.Object, _mockWsService.Object, _mockLogger.Object));

        Assert.Throws<ArgumentNullException>(() =>
            new WsRequestProcessor(_mockConnectionStateService.Object, null, _mockWsService.Object, _mockLogger.Object));

        Assert.Throws<ArgumentNullException>(() =>
            new WsRequestProcessor(_mockConnectionStateService.Object, _mockConnectionFactory.Object, null, _mockLogger.Object));

        Assert.Throws<ArgumentNullException>(() =>
            new WsRequestProcessor(_mockConnectionStateService.Object, _mockConnectionFactory.Object, _mockWsService.Object, null));
    }

    [Fact]
    public void IsWebSocketRequest_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new WsMiddlewareOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _processor.IsWebSocketRequest(null, options));
    }

    [Fact]
    public void IsWebSocketRequest_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _processor.IsWebSocketRequest(context, null));
    }

    [Fact]
    public void IsWebSocketRequest_WithWebSocketRequestAndMatchingPath_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var options = new WsMiddlewareOptions { Paths = new PathSettings { RequestPath = "/ws" } };

        var mockWebSocketManager = new Mock<WebSocketManager>();
        mockWebSocketManager.Setup(m => m.IsWebSocketRequest).Returns(true);
        context.Request.Path = "/ws";
        //context.RequestServices = new ServiceCollection().BuildServiceProvider();
        //context.WebSockets = mockWebSocketManager.Object;

        // Act
        var result = _processor.IsWebSocketRequest(context, options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWebSocketRequest_WithNonWebSocketRequest_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var options = new WsMiddlewareOptions { Paths = new PathSettings { RequestPath = "/ws" } };

        var mockWebSocketManager = new Mock<WebSocketManager>();
        mockWebSocketManager.Setup(m => m.IsWebSocketRequest).Returns(false);
        context.Request.Path = "/ws";
        //context.WebSockets = mockWebSocketManager.Object;

        // Act
        var result = _processor.IsWebSocketRequest(context, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWebSocketRequest_WithNonMatchingPath_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var options = new WsMiddlewareOptions { Paths = new PathSettings { RequestPath = "/ws" } };

        var mockWebSocketManager = new Mock<WebSocketManager>();
        mockWebSocketManager.Setup(m => m.IsWebSocketRequest).Returns(true);
        context.Request.Path = "/different-path";
        //context.WebSockets = mockWebSocketManager.Object;

        // Act
        var result = _processor.IsWebSocketRequest(context, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HandleWebSocketRequestAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new WsMiddlewareOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _processor.HandleWebSocketRequestAsync(null, options));
    }

    [Fact]
    public async Task HandleWebSocketRequestAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _processor.HandleWebSocketRequestAsync(context, null));
    }

    [Fact]
    public async Task HandleWebSocketRequestAsync_WithNonMatchingPath_Returns404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var options = new WsMiddlewareOptions { Paths = new PathSettings { RequestPath = "/ws" } };

        var mockWebSocketManager = new Mock<WebSocketManager>();
        mockWebSocketManager.Setup(m => m.IsWebSocketRequest).Returns(true);
        context.Request.Path = "/different-path";
        context.Response.Body = new MemoryStream();
        //context.WebSockets = mockWebSocketManager.Object;

        // Act
        await _processor.HandleWebSocketRequestAsync(context, options);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("text/plain; charset=utf-8", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = new StreamReader(context.Response.Body).ReadToEnd();
        Assert.Equal("Not found", responseBody);
    }

    [Fact]
    public async Task HandleWebSocketRequestAsync_WithMissingConnectionToken_Returns400()
    {
        // Arrange
        var context = CreateWebSocketContext("/ws");
        var options = new WsMiddlewareOptions { Paths = new PathSettings { RequestPath = "/ws" } };

        _mockConnectionFactory.Setup(f => f.GetConnectionTokenId(context))
            .Returns(string.Empty);

        // Act
        await _processor.HandleWebSocketRequestAsync(context, options);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = new StreamReader(context.Response.Body).ReadToEnd();
        Assert.Equal("Missing connection token", responseBody);

        // Verify logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Missing connection token")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebSocketRequestAsync_WithInvalidConnectionToken_Returns400()
    {
        // Arrange
        var context = CreateWebSocketContext("/ws");
        var options = new WsMiddlewareOptions { Paths = new PathSettings { RequestPath = "/ws" } };
        var connectionTokenId = "invalid-token";

        _mockConnectionFactory.Setup(f => f.GetConnectionTokenId(context))
            .Returns(connectionTokenId);
        _mockConnectionStateService.Setup(s => s.GetAsync(connectionTokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConnectionSettings)null);

        // Act
        await _processor.HandleWebSocketRequestAsync(context, options);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = new StreamReader(context.Response.Body).ReadToEnd();
        Assert.Equal("Invalid connection token", responseBody);

        // Verify logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid connection token")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebSocketRequestAsync_WithValidConnection_SetsUserContext()
    {
        // Arrange
        var context = CreateWebSocketContext("/ws");
        var options = new WsMiddlewareOptions { Paths = new PathSettings { RequestPath = "/ws" } };
        var connectionTokenId = "valid-token";
        var userName = "testuser";

        var user = new ClaimsPrincipal(new GenericIdentity(userName));
        var connectionOptions = new ConnectionSettings { User = user };

        _mockConnectionFactory.Setup(f => f.GetConnectionTokenId(context))
            .Returns(connectionTokenId);
        _mockConnectionStateService.Setup(s => s.GetAsync(connectionTokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connectionOptions);

        // Act
        await _processor.HandleWebSocketRequestAsync(context, options);

        // Assert
        Assert.Same(user, context.User);
        Assert.Equal(userName, context.User.Identity.Name);

        // Verify logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("User context set") && v.ToString().Contains(userName)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebSocketRequestAsync_WithAuthorizationRequiredAndUnauthenticatedUser_Returns401()
    {
        // Arrange
        var context = CreateWebSocketContext("/ws");
        var options = new WsMiddlewareOptions
        {
            Paths = new PathSettings { RequestPath = "/ws" },
            Authorization = new AuthorizationSettings { RequireAuthorization = true }
        };
        var connectionTokenId = "valid-token";

        var user = new ClaimsPrincipal(new GenericIdentity("")); // Unauthenticated
        var connectionOptions = new ConnectionSettings { User = user };

        _mockConnectionFactory.Setup(f => f.GetConnectionTokenId(context))
            .Returns(connectionTokenId);
        _mockConnectionStateService.Setup(s => s.GetAsync(connectionTokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connectionOptions);

        // Act
        await _processor.HandleWebSocketRequestAsync(context, options);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = new StreamReader(context.Response.Body).ReadToEnd();
        Assert.Equal("Unauthorized", responseBody);

        // Verify logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("WebSocket request auth failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleWebSocketRequestAsync_WithValidRequest_CallsWsService()
    {
        // Arrange
        var context = CreateWebSocketContext("/ws");
        var options = new WsMiddlewareOptions
        {
            Paths = new PathSettings { RequestPath = "/ws" },
            Authorization = new AuthorizationSettings { RequireAuthorization = false }
        };
        var connectionTokenId = "valid-token";

        var user = new ClaimsPrincipal(new GenericIdentity("testuser"));
        var connectionOptions = new ConnectionSettings { User = user };

        _mockConnectionFactory.Setup(f => f.GetConnectionTokenId(context))
            .Returns(connectionTokenId);
        _mockConnectionStateService.Setup(s => s.GetAsync(connectionTokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connectionOptions);

        _mockWsService.Setup(s => s.HandleWebSocketRequestAsync(context, It.IsAny<WsMiddlewareOptions>()))
            .Returns(Task.CompletedTask);

        // Act
        await _processor.HandleWebSocketRequestAsync(context, options);

        // Assert
        _mockWsService.Verify(s => s.HandleWebSocketRequestAsync(context, It.Is<WsMiddlewareOptions>(o =>
            o.Connection == connectionOptions)), Times.Once);
    }

    [Fact]
    public async Task HandleWebSocketRequestAsync_WithNullUserInConnectionOptions_DoesNotSetUserContext()
    {
        // Arrange
        var context = CreateWebSocketContext("/ws");
        var originalUser = context.User;
        var options = new WsMiddlewareOptions { Paths = new PathSettings { RequestPath = "/ws" } };
        var connectionTokenId = "valid-token";

        var connectionOptions = new ConnectionSettings { User = null };

        _mockConnectionFactory.Setup(f => f.GetConnectionTokenId(context))
            .Returns(connectionTokenId);
        _mockConnectionStateService.Setup(s => s.GetAsync(connectionTokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connectionOptions);

        _mockWsService.Setup(s => s.HandleWebSocketRequestAsync(context, It.IsAny<WsMiddlewareOptions>()))
            .Returns(Task.CompletedTask);

        // Act
        await _processor.HandleWebSocketRequestAsync(context, options);

        // Assert
        Assert.Same(originalUser, context.User); // User should remain unchanged
    }

    [Fact]
    public async Task HandleWebSocketRequestAsync_WithAuthenticatedUserAndNoAuthorizationRequired_ProcessesRequest()
    {
        // Arrange
        var context = CreateWebSocketContext("/ws");
        var options = new WsMiddlewareOptions
        {
            Paths = new PathSettings { RequestPath = "/ws" },
            Authorization = new AuthorizationSettings { RequireAuthorization = false }
        };
        var connectionTokenId = "valid-token";

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, "testuser")
        }, "TestAuth"));
        var connectionOptions = new ConnectionSettings { User = user };

        _mockConnectionFactory.Setup(f => f.GetConnectionTokenId(context))
            .Returns(connectionTokenId);
        _mockConnectionStateService.Setup(s => s.GetAsync(connectionTokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connectionOptions);

        _mockWsService.Setup(s => s.HandleWebSocketRequestAsync(context, It.IsAny<WsMiddlewareOptions>()))
            .Returns(Task.CompletedTask);

        // Act
        await _processor.HandleWebSocketRequestAsync(context, options);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode); // Default status code
        _mockWsService.Verify(s => s.HandleWebSocketRequestAsync(context, It.IsAny<WsMiddlewareOptions>()), Times.Once);
    }

    private static HttpContext CreateWebSocketContext(string path)
    {
        var context = new DefaultHttpContext();
        var mockWebSocketManager = new Mock<WebSocketManager>();
        mockWebSocketManager.Setup(m => m.IsWebSocketRequest).Returns(true);

        context.Request.Path = path;
        //context.WebSockets = mockWebSocketManager.Object;
        context.Response.Body = new MemoryStream();

        return context;
    }
}

// Logger extension method for verification
public static class TestLoggerExtensions
{
    public static void MissingConnectionToken(this ILogger logger, string connectionId)
    {
        logger.LogInformation("Missing connection token for connection {ConnectionId}", connectionId);
    }

    public static void InvalidConnectionToken(this ILogger logger, string connectionTokenId)
    {
        logger.LogInformation("Invalid connection token {ConnectionTokenId}", connectionTokenId);
    }

    public static void UserContextSet(this ILogger logger, string userName)
    {
        logger.LogInformation("User context set for user {UserName}", userName);
    }

    public static void WebSocketRequestAuthFailed(this ILogger logger, string connectionId)
    {
        logger.LogInformation("WebSocket request auth failed for connection {ConnectionId}", connectionId);
    }
}
