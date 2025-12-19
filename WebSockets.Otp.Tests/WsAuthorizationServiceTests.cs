using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Results;
using WebSockets.Otp.Core.Authorization;

namespace WebSockets.Otp.Tests;

public class WsAuthorizationServiceTests
{
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly Mock<ILogger<WsAuthorizationService>> _mockLogger;
    private readonly WsAuthorizationService _service;

    public WsAuthorizationServiceTests()
    {
        _mockAuthorizationService = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<WsAuthorizationService>>();
        _service = new WsAuthorizationService(_mockAuthorizationService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenOptionsNull_ReturnsSuccess()
    {
        // Arrange
        var context = CreateHttpContext(authenticated: true);

        // Act
        var result = await _service.Auhtorize(context, null);

        // Assert
        Assert.True(result.Succeeded);
        _mockLogger.VerifyLog(LogLevel.Information, "Authorization not required");
    }

    [Fact]
    public async Task AuthorizeAsync_WhenRequireAuthorizationFalse_ReturnsSuccess()
    {
        // Arrange
        var context = CreateHttpContext(authenticated: true);
        var options = new Abstractions.Options.WsAuthorizationOptions { RequireAuthorization = false };

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.True(result.Succeeded);
        _mockLogger.VerifyLog(LogLevel.Information, "Authorization not required");
    }

    [Fact]
    public async Task AuthorizeAsync_WhenAuthorizationServiceNull_ReturnsFailure()
    {
        // Arrange
        var service = new WsAuthorizationService(null, _mockLogger.Object);
        var context = CreateHttpContext(authenticated: true);
        var options = new Abstractions.Options.WsAuthorizationOptions { RequireAuthorization = true };

        // Act
        var result = await service.Auhtorize(context, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Authorization service not provided", result.FailureReason);
        _mockLogger.VerifyLog(LogLevel.Error, "Authorization service missing");
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserNotAuthenticated_ReturnsFailure()
    {
        // Arrange
        var context = CreateHttpContext(authenticated: false);
        var options = new Abstractions.Options.WsAuthorizationOptions { RequireAuthorization = true };

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User is not authenticated", result.FailureReason);
        _mockLogger.VerifyLog(LogLevel.Warning, "User not authenticated");
    }

    //[Fact]
    //public async Task AuthorizeAsync_WhenPolicyAuthorizationFails_ReturnsFailure()
    //{
    //    // Arrange
    //    var context = CreateHttpContext(authenticated: true);
    //    var options = new AuthorizationSettings
    //    {
    //        RequireAuthorization = true,
    //        Policies = new[] { "Policy1", "Policy2" }
    //    };

    //    _mockAuthorizationService
    //        .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), "Policy1"))
    //        .ReturnsAsync(AuthorizationResult.Success());

    //    _mockAuthorizationService
    //        .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), "Policy2"))
    //        .ReturnsAsync(AuthorizationResult.Failed("Access denied"));

    //    // Act
    //    var result = await _service.AuhtorizeAsync(context, options);

    //    // Assert
    //    Assert.False(result.Succeeded);
    //    Assert.Equal("Access denied", result.FailureReason);
    //    _mockLogger.VerifyLog(LogLevel.Warning, "Policy authorization failed");
    //}

    [Fact]
    public async Task AuthorizeAsync_WhenPolicyAuthorizationFailsWithNullFailure_ReturnsGenericMessage()
    {
        // Arrange
        var context = CreateHttpContext(authenticated: true);
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Policies = new[] { "TestPolicy" }
        };

        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), "TestPolicy"))
            .ReturnsAsync(AuthorizationResult.Failed());

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Policy 'TestPolicy': unknown authorization failure", result.FailureReason);
        _mockLogger.VerifyLog(LogLevel.Warning, "Policy authorization failed");
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserNotInRequiredRole_ReturnsFailure()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "User")
        }, "TestAuth"));

        var context = new DefaultHttpContext { User = user };
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Roles = new[] { "Admin", "Manager" }
        };

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User is not in the required role", result.FailureReason);
        _mockLogger.VerifyLog(LogLevel.Warning, "User not in required role");
    }

    [Fact]
    public async Task AuthorizeAsync_WhenInvalidAuthorizationScheme_ReturnsFailure()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser")
        }, "Bearer"));

        var context = new DefaultHttpContext { User = user };
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Schemes = new[] { "Cookies", "CustomAuth" }
        };

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User authorization scheme is not valid", result.FailureReason);
        _mockLogger.VerifyLog(LogLevel.Warning, "Invalid authorization scheme");
    }

    [Fact]
    public async Task AuthorizeAsync_WhenAllChecksPass_ReturnsSuccess()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Cookies"));

        var context = new DefaultHttpContext { User = user };
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Policies = new[] { "Policy1" },
            Roles = new[] { "Admin" },
            Schemes = new[] { "Cookies" }
        };

        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), "Policy1"))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.True(result.Succeeded);
        _mockLogger.VerifyLog(LogLevel.Information, "Authorization succeeded");
    }

    [Fact]
    public async Task AuthorizeAsync_WhenPoliciesNull_DoesNotEvaluatePolicies()
    {
        // Arrange
        var context = CreateHttpContext(authenticated: true);
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Policies = null,
            Roles = new[] { "Admin" }
        };

        var user = context.User;
        user.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }));

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.True(result.Succeeded);
        _mockAuthorizationService.Verify(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenPoliciesEmpty_DoesNotEvaluatePolicies()
    {
        // Arrange
        var context = CreateHttpContext(authenticated: true);
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Policies = Array.Empty<string>(),
            Roles = new[] { "Admin" }
        };

        var user = context.User;
        user.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }));

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.True(result.Succeeded);
        _mockAuthorizationService.Verify(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenRolesNull_DoesNotCheckRoles()
    {
        // Arrange
        var context = CreateHttpContext(authenticated: true);
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Policies = new[] { "Policy1" },
            Roles = null
        };

        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), "Policy1"))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenRolesEmpty_DoesNotCheckRoles()
    {
        // Arrange
        var context = CreateHttpContext(authenticated: true);
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Policies = new[] { "Policy1" },
            Roles = Array.Empty<string>()
        };

        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), "Policy1"))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenSchemesNull_DoesNotCheckSchemes()
    {
        // Arrange
        var context = CreateHttpContext(authenticated: true);
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Policies = new[] { "Policy1" },
            Schemes = null
        };

        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), "Policy1"))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenSchemesEmpty_DoesNotCheckSchemes()
    {
        // Arrange
        var context = CreateHttpContext(authenticated: true);
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Policies = new[] { "Policy1" },
            Schemes = Array.Empty<string>()
        };

        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), "Policy1"))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserHasRequiredRole_ReturnsSuccess()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "User")
        }, "TestAuth"));

        var context = new DefaultHttpContext { User = user };
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Roles = new[] { "Admin", "Manager" }
        };

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserHasMatchingScheme_ReturnsSuccess()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser")
        }, "Cookies"));

        var context = new DefaultHttpContext { User = user };
        var options = new Abstractions.Options.WsAuthorizationOptions
        {
            RequireAuthorization = true,
            Schemes = new[] { "Cookies", "Bearer" }
        };

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserIdentityNull_ReturnsFailure()
    {
        // Arrange
        var context = new DefaultHttpContext { User = new ClaimsPrincipal() };
        var options = new Abstractions.Options.WsAuthorizationOptions { RequireAuthorization = true };

        // Act
        var result = await _service.Auhtorize(context, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User is not authenticated", result.FailureReason);
    }

    [Fact]
    public async Task AuthorizeAsync_VirtualMethod_CanBeOverridden()
    {
        // Arrange
        var service = new TestableWsAuthorizationService(_mockAuthorizationService.Object, _mockLogger.Object);
        var context = CreateHttpContext(authenticated: true);
        var options = new Abstractions.Options.WsAuthorizationOptions { RequireAuthorization = true };

        // Act
        var result = await service.Auhtorize(context, options);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(service.WasOverriddenCalled);
    }

    private static HttpContext CreateHttpContext(bool authenticated)
    {
        var identity = authenticated ? new ClaimsIdentity() : new ClaimsIdentity("Test");
        if (authenticated)
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        }

        var user = new ClaimsPrincipal(identity);
        return new DefaultHttpContext { User = user };
    }
}

// Testable implementation for testing virtual method
public class TestableWsAuthorizationService : WsAuthorizationService
{
    public bool WasOverriddenCalled { get; private set; }

    public TestableWsAuthorizationService(IAuthorizationService authorizationService, ILogger<WsAuthorizationService> logger)
        : base(authorizationService, logger)
    {
    }

    public override async Task<WsAuthorizationResult> Auhtorize(HttpContext context, Abstractions.Options.WsAuthorizationOptions options)
    {
        WasOverriddenCalled = true;
        return WsAuthorizationResult.Success();
    }
}

public static class LoggerWsAuthExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> mockLogger, LogLevel level, string expectedMessage)
    {
        mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}

public static class LoggerExtensionsImplementation
{
    public static void LogAuthorizationNotRequired(this ILogger logger) { }
    public static void LogAuthorizationServiceMissing(this ILogger logger) { }
    public static void LogUserNotAuthenticated(this ILogger logger) { }
    public static void LogPolicyAuthorizationFailed(this ILogger logger, string policy, string reason) { }
    public static void LogUserNotInRequiredRole(this ILogger logger) { }
    public static void LogInvalidAuthorizationScheme(this ILogger logger) { }
    public static void LogAuthorizationSucceeded(this ILogger logger) { }
}
