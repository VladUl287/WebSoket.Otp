using FluentAssertions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core;

namespace WebSockets.Otp.Tests;

// Test types for registration
[WsEndpoint("test-endpoint-1")]
public sealed class TestEndpoint1 : IWsEndpoint { }

[WsEndpoint("test-endpoint-2")]
public sealed class TestEndpoint2 : IWsEndpoint { }

[WsEndpoint("duplicate-key")]
public sealed class TestEndpoint3 : IWsEndpoint { }

// Invalid type - missing WsEndpointAttribute
public sealed class MissingAttributeEndpoint : IWsEndpoint { }

// Invalid type - has attribute but doesn't implement IWsEndpoint
[WsEndpoint("invalid-type")]
public sealed class InvalidTypeEndpoint { }

public class WsEndpointRegistryTests
{
    private readonly WsEndpointRegistry _registry;

    public WsEndpointRegistryTests()
    {
        _registry = new WsEndpointRegistry();
    }

    [Fact]
    public void Resolve_WhenEndpointExists_ReturnsCorrectType()
    {
        // Arrange
        var endpoints = new[] { typeof(TestEndpoint1), typeof(TestEndpoint2) };
        _registry.Register(endpoints);

        // Act
        var result = _registry.Resolve("test-endpoint-1");

        // Assert
        result.Should().Be(typeof(TestEndpoint1));
    }

    [Fact]
    public void Resolve_WhenEndpointDoesNotExist_ReturnsNull()
    {
        // Arrange
        var endpoints = new[] { typeof(TestEndpoint1) };
        _registry.Register(endpoints);

        // Act
        var result = _registry.Resolve("non-existent-endpoint");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenRegistryIsEmpty_ReturnsNull()
    {
        // Arrange - no registration

        // Act
        var result = _registry.Resolve("any-endpoint");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Enumerate_WhenEndpointsRegistered_ReturnsAllTypes()
    {
        // Arrange
        var endpoints = new[] { typeof(TestEndpoint1), typeof(TestEndpoint2) };
        _registry.Register(endpoints);

        // Act
        var result = _registry.Enumerate();

        // Assert
        result.Should().BeEquivalentTo(endpoints);
    }

    [Fact]
    public void Enumerate_WhenRegistryIsEmpty_ReturnsEmptyCollection()
    {
        // Arrange - no registration

        // Act
        var result = _registry.Enumerate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Register_WithValidTypes_AddsAllEndpoints()
    {
        // Arrange
        var endpoints = new[] { typeof(TestEndpoint1), typeof(TestEndpoint2) };

        // Act
        _registry.Register(endpoints);

        // Assert
        _registry.Resolve("test-endpoint-1").Should().Be(typeof(TestEndpoint1));
        _registry.Resolve("test-endpoint-2").Should().Be(typeof(TestEndpoint2));
        _registry.Enumerate().Should().HaveCount(2);
    }

    [Fact]
    public void Register_WithDuplicateKeys_OverwritesPreviousRegistration()
    {
        // Arrange
        var firstRegistration = new[] { typeof(TestEndpoint1) };
        var secondRegistration = new[] { typeof(TestEndpoint3) }; // Same key as TestEndpoint1

        // Act
        _registry.Register(firstRegistration);
        _registry.Register(secondRegistration);

        // Assert
        _registry.Resolve("duplicate-key").Should().Be(typeof(TestEndpoint3));
        _registry.Enumerate().Should().ContainSingle();
    }

    [Fact]
    public void Register_WithTypeMissingAttribute_ThrowsArgumentException()
    {
        // Arrange
        var endpoints = new[] { typeof(MissingAttributeEndpoint) };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _registry.Register(endpoints));
        exception.Message.Should().Contain("Endpoint type must be annotated with [WsEndpoint(\"key\")]");
    }

    [Fact]
    public void Register_WithInvalidType_ThrowsArgumentException()
    {
        // Arrange
        var endpoints = new[] { typeof(InvalidTypeEndpoint) };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _registry.Register(endpoints));
        exception.Message.Should().Contain("Type is not correct endpoint type");
    }

    [Fact]
    public void Register_WithMixedValidAndInvalidTypes_ThrowsOnFirstInvalid()
    {
        // Arrange
        var endpoints = new[] { typeof(TestEndpoint1), typeof(InvalidTypeEndpoint), typeof(TestEndpoint2) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _registry.Register(endpoints));

        // Verify no endpoints were registered when exception occurs
        _registry.Enumerate().Should().BeEmpty();
    }

    [Fact]
    public void Register_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var endpoints = new Type[] { null! };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _registry.Register(endpoints));
    }

    [Fact]
    public void Register_WithEmptyCollection_DoesNothing()
    {
        // Arrange
        var endpoints = Enumerable.Empty<Type>();

        // Act
        _registry.Register(endpoints);

        // Assert
        _registry.Enumerate().Should().BeEmpty();
    }

    [Fact]
    public void Register_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<Type> endpoints = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _registry.Register(endpoints));
    }

    [Fact]
    public void Register_MultipleTimes_ReplacesPreviousRegistry()
    {
        // Arrange
        var firstBatch = new[] { typeof(TestEndpoint1), typeof(TestEndpoint2) };
        var secondBatch = new[] { typeof(TestEndpoint3) };

        // Act
        _registry.Register(firstBatch);
        _registry.Register(secondBatch);

        // Assert
        _registry.Enumerate().Should().ContainSingle();
        _registry.Resolve("test-endpoint-1").Should().BeNull();
        _registry.Resolve("duplicate-key").Should().Be(typeof(TestEndpoint3));
    }

    [Fact]
    public void Resolve_WithNullPath_ReturnsNull()
    {
        // Arrange
        var endpoints = new[] { typeof(TestEndpoint1) };
        _registry.Register(endpoints);

        // Act
        var result = _registry.Resolve(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WithEmptyPath_ReturnsNull()
    {
        // Arrange
        var endpoints = new[] { typeof(TestEndpoint1) };
        _registry.Register(endpoints);

        // Act
        var result = _registry.Resolve("");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("test-endpoint-1")]
    [InlineData("TEST-ENDPOINT-1")] // case sensitivity test
    [InlineData(" test-endpoint-1 ")] // whitespace test
    public void Resolve_WithDifferentPathVariations_RespectsExactMatching(string path)
    {
        // Arrange
        var endpoints = new[] { typeof(TestEndpoint1) };
        _registry.Register(endpoints);

        // Act
        var result = _registry.Resolve(path);

        // Assert - should only match exact key "test-endpoint-1"
        if (path == "test-endpoint-1")
        {
            result.Should().Be(typeof(TestEndpoint1));
        }
        else
        {
            result.Should().BeNull();
        }
    }
}