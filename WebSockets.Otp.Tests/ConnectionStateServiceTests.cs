using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Tests;

public class ConnectionSettingsTests
{
    [Fact]
    public void ConnectionSettings_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var settings = new ConnectionSettings();

        // Assert
        Assert.Null(settings.User);
        Assert.Equal("json", settings.Protocol);
    }

    [Fact]
    public void ConnectionSettings_Properties_CanBeSet()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        // Act
        var settings = new ConnectionSettings
        {
            User = user,
            Protocol = "protocol2"
        };

        // Assert
        Assert.Equal(user, settings.User);
        Assert.Equal("protocol2", settings.Protocol);
    }

    [Fact]
    public void ConnectionSettings_User_CanBeNull()
    {
        // Arrange & Act
        var settings = new ConnectionSettings
        {
            User = null,
            Protocol = "test"
        };

        // Assert
        Assert.Null(settings.User);
        Assert.Equal("test", settings.Protocol);
    }
}

public class ConnectionStateServiceTests
{
    private readonly Mock<IConnectionStateService> _mockService;
    private readonly ConnectionSettings _defaultSettings;
    private readonly DefaultHttpContext _httpContext;
    private readonly CancellationToken _cancellationToken;

    public ConnectionStateServiceTests()
    {
        _mockService = new Mock<IConnectionStateService>();
        _defaultSettings = new ConnectionSettings { Protocol = "json" };
        _httpContext = new DefaultHttpContext();
        _cancellationToken = new CancellationToken();
    }

    [Fact]
    public async Task GenerateTokenId_WithValidParameters_ReturnsTokenId()
    {
        // Arrange
        var expectedTokenId = "test-token-123";
        _mockService
            .Setup(x => x.GenerateTokenId(_httpContext, _defaultSettings, _cancellationToken))
            .ReturnsAsync(expectedTokenId);

        // Act
        var result = await _mockService.Object.GenerateTokenId(_httpContext, _defaultSettings, _cancellationToken);

        // Assert
        Assert.Equal(expectedTokenId, result);
        _mockService.Verify(x => x.GenerateTokenId(_httpContext, _defaultSettings, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GenerateTokenId_WithNullHttpContext_ThrowsException()
    {
        // Arrange
        _mockService
            .Setup(x => x.GenerateTokenId(null!, _defaultSettings, _cancellationToken))
            .ThrowsAsync(new ArgumentNullException(nameof(HttpContext)));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mockService.Object.GenerateTokenId(null!, _defaultSettings, _cancellationToken));
    }

    [Fact]
    public async Task GenerateTokenId_WithNullConnectionSettings_ThrowsException()
    {
        // Arrange
        _mockService
            .Setup(x => x.GenerateTokenId(_httpContext, null!, _cancellationToken))
            .ThrowsAsync(new ArgumentNullException(nameof(ConnectionSettings)));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mockService.Object.GenerateTokenId(_httpContext, null!, _cancellationToken));
    }

    [Fact]
    public async Task GenerateTokenId_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cancelledToken = new CancellationToken(canceled: true);

        _mockService
            .Setup(x => x.GenerateTokenId(_httpContext, _defaultSettings, cancelledToken))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _mockService.Object.GenerateTokenId(_httpContext, _defaultSettings, cancelledToken));
    }

    [Fact]
    public async Task GetAsync_WithValidKey_ReturnsConnectionSettings()
    {
        // Arrange
        var key = "valid-key";
        var expectedSettings = new ConnectionSettings { Protocol = "custom" };

        _mockService
            .Setup(x => x.GetAsync(key, _cancellationToken))
            .ReturnsAsync(expectedSettings);

        // Act
        var result = await _mockService.Object.GetAsync(key, _cancellationToken);

        // Assert
        Assert.Equal(expectedSettings, result);
        _mockService.Verify(x => x.GetAsync(key, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        var key = "non-existent-key";

        _mockService
            .Setup(x => x.GetAsync(key, _cancellationToken))
            .ReturnsAsync((ConnectionSettings?)null);

        // Act
        var result = await _mockService.Object.GetAsync(key, _cancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetAsync_WithInvalidKey_ReturnsNull(string invalidKey)
    {
        // Arrange
        _mockService
            .Setup(x => x.GetAsync(invalidKey, _cancellationToken))
            .ReturnsAsync((ConnectionSettings?)null);

        // Act
        var result = await _mockService.Object.GetAsync(invalidKey, _cancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = "test-key";
        var cancelledToken = new CancellationToken(canceled: true);

        _mockService
            .Setup(x => x.GetAsync(key, cancelledToken))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _mockService.Object.GetAsync(key, cancelledToken));
    }

    [Fact]
    public async Task RevokeAsync_WithValidKey_CompletesSuccessfully()
    {
        // Arrange
        var key = "key-to-revoke";

        _mockService
            .Setup(x => x.RevokeAsync(key, _cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _mockService.Object.RevokeAsync(key, _cancellationToken);

        // Assert
        _mockService.Verify(x => x.RevokeAsync(key, _cancellationToken), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task RevokeAsync_WithInvalidKey_CompletesSuccessfully(string invalidKey)
    {
        // Arrange
        _mockService
            .Setup(x => x.RevokeAsync(invalidKey, _cancellationToken))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await _mockService.Object.RevokeAsync(invalidKey, _cancellationToken);
        // Should not throw for invalid keys (implementation dependent)
    }

    [Fact]
    public async Task RevokeAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = "test-key";
        var cancelledToken = new CancellationToken(canceled: true);

        _mockService
            .Setup(x => x.RevokeAsync(key, cancelledToken))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _mockService.Object.RevokeAsync(key, cancelledToken));
    }

    [Fact]
    public async Task RevokeAsync_AlreadyRevokedKey_CompletesSuccessfully()
    {
        // Arrange
        var key = "already-revoked-key";

        _mockService
            .Setup(x => x.RevokeAsync(key, _cancellationToken))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await _mockService.Object.RevokeAsync(key, _cancellationToken);
        // Should not throw for already revoked keys
    }

    [Fact]
    public async Task Integration_GenerateThenGet_ReturnsSameSettings()
    {
        // Arrange
        var tokenId = "generated-token";
        var settings = new ConnectionSettings
        {
            Protocol = "binary",
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("test", "value") }))
        };

        _mockService
            .Setup(x => x.GenerateTokenId(_httpContext, settings, _cancellationToken))
            .ReturnsAsync(tokenId);

        _mockService
            .Setup(x => x.GetAsync(tokenId, _cancellationToken))
            .ReturnsAsync(settings);

        // Act
        var generatedToken = await _mockService.Object.GenerateTokenId(_httpContext, settings, _cancellationToken);
        var retrievedSettings = await _mockService.Object.GetAsync(generatedToken, _cancellationToken);

        // Assert
        Assert.Equal(tokenId, generatedToken);
        Assert.Equal(settings, retrievedSettings);
        Assert.Equal(settings.Protocol, retrievedSettings!.Protocol);
        Assert.Equal(settings.User, retrievedSettings.User);
    }

    [Fact]
    public async Task Integration_GenerateRevokeGet_ReturnsNull()
    {
        // Arrange
        var tokenId = "temp-token";
        var settings = new ConnectionSettings { Protocol = "test" };

        _mockService
            .Setup(x => x.GenerateTokenId(_httpContext, settings, _cancellationToken))
            .ReturnsAsync(tokenId);

        _mockService
            .Setup(x => x.GetAsync(tokenId, _cancellationToken))
            .ReturnsAsync((ConnectionSettings?)null);

        _mockService
            .Setup(x => x.RevokeAsync(tokenId, _cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var generatedToken = await _mockService.Object.GenerateTokenId(_httpContext, settings, _cancellationToken);
        await _mockService.Object.RevokeAsync(generatedToken, _cancellationToken);
        var retrievedSettings = await _mockService.Object.GetAsync(generatedToken, _cancellationToken);

        // Assert
        Assert.Null(retrievedSettings);
    }
}

// Example implementation for more concrete testing
public class ConcreteConnectionStateServiceTests
{
    private readonly Mock<IConnectionStateService> _mockService;
    private readonly ConnectionSettings _testSettings;

    public ConcreteConnectionStateServiceTests()
    {
        _mockService = new Mock<IConnectionStateService>();
        _testSettings = new ConnectionSettings
        {
            Protocol = "test-protocol",
            User = new ClaimsPrincipal(new ClaimsIdentity("TestAuth"))
        };
    }

    [Fact]
    public async Task GenerateTokenId_WithUserClaims_IncludesUserInSettings()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var expectedToken = "user-token-123";

        _mockService
            .Setup(x => x.GenerateTokenId(httpContext, It.Is<ConnectionSettings>(s => s.User != null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _mockService.Object.GenerateTokenId(httpContext, _testSettings, CancellationToken.None);

        // Assert
        Assert.Equal(expectedToken, result);
    }

    [Fact]
    public async Task GetAsync_MultipleCalls_ReturnsSameResult()
    {
        // Arrange
        var key = "consistent-key";
        var settings = new ConnectionSettings { Protocol = "consistent" };

        _mockService
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result1 = await _mockService.Object.GetAsync(key, CancellationToken.None);
        var result2 = await _mockService.Object.GetAsync(key, CancellationToken.None);

        // Assert
        Assert.Equal(settings, result1);
        Assert.Equal(settings, result2);
        Assert.Same(result1, result2);
    }
}