using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core;

namespace WebSockets.Otp.Tests;

public class TestEndpoint
{
    public bool WasInvoked { get; set; }
    public object? ReceivedRequest { get; set; }
}

public class TestRequestEndpoint : TestEndpoint
{
    public Task HandleAsync(TestRequest request, IWsExecutionContext ctx, CancellationToken ct)
    {
        WasInvoked = true;
        ReceivedRequest = request;
        return Task.CompletedTask;
    }
}

public class TestConnectionEndpoint : TestEndpoint
{
    public Task HandleAsync(IWsExecutionContext ctx, CancellationToken ct)
    {
        WasInvoked = true;
        return Task.CompletedTask;
    }
}

public class TestRequest
{
    public string Message { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class TestExecutionContext : IWsExecutionContext
{
    public string Key { get; set; } = "test-key";
    public IWsConnection Connection { get; set; } = Mock.Of<IWsConnection>();
    public CancellationToken Cancellation { get; set; } = CancellationToken.None;
    public ISerializer Serializer { get; set; } = Mock.Of<ISerializer>();
    public IMessageBuffer RawPayload { get; set; } = Mock.Of<IMessageBuffer>();
    public Type Endpoint { get; set; } = typeof(object);
}

public static class LoggerExtensions
{
    public static void VerifyLogInvokingEndpoint<T>(this Mock<ILogger<T>> loggerMock, string endpointName, string connectionId, Times times)
    {
        loggerMock.Verify(logger => logger.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Invoking endpoint {endpointName} for connection {connectionId}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            times);
    }

    public static void VerifyLogInvocationSuccess<T>(this Mock<ILogger<T>> loggerMock, string endpointName, string connectionId, Times times)
    {
        loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully invoked endpoint {endpointName} for connection {connectionId}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            times);
    }

    public static void VerifyLogInvocationFailed<T>(this Mock<ILogger<T>> loggerMock, string endpointName, string connectionId, Times times)
    {
        loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to invoke endpoint {endpointName} for connection {connectionId}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            times);
    }

    public static void VerifyLogCacheHit<T>(this Mock<ILogger<T>> loggerMock, string endpointName, Times times)
    {
        loggerMock.Verify(logger => logger.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Cache hit for endpoint type {endpointName}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            times);
    }

    public static void VerifyLogCacheMiss<T>(this Mock<ILogger<T>> loggerMock, string endpointName, Times times)
    {
        loggerMock.Verify(logger => logger.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Cache miss for endpoint type {endpointName}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            times);
    }
}

public class EndpointInvokerTests
{
    private readonly Mock<IMethodResolver> _methodResolverMock;
    private readonly Mock<ILogger<EndpointInvoker>> _loggerMock;
    private readonly EndpointInvoker _invoker;

    public EndpointInvokerTests()
    {
        _methodResolverMock = new Mock<IMethodResolver>();
        _loggerMock = new Mock<ILogger<EndpointInvoker>>();
        _invoker = new EndpointInvoker(_methodResolverMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullMethodResolver_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EndpointInvoker(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EndpointInvoker(_methodResolverMock.Object, null!));
    }

    [Fact]
    public async Task InvokeEndpointAsync_WithNullEndpointInstance_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new TestExecutionContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _invoker.InvokeEndpointAsync(null!, context, CancellationToken.None));
    }

    [Fact]
    public async Task InvokeEndpointAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var endpoint = new TestConnectionEndpoint();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _invoker.InvokeEndpointAsync(endpoint, null!, CancellationToken.None));
    }

    [Fact]
    public async Task InvokeEndpointAsync_WithConnectionEndpoint_InvokesSuccessfully()
    {
        // Arrange
        var endpoint = new TestConnectionEndpoint();
        var context = new TestExecutionContext();
        var connectionMock = new Mock<IWsConnection>();
        connectionMock.Setup(c => c.Id).Returns("conn-123");
        context.Connection = connectionMock.Object;

        var handleMethod = typeof(TestConnectionEndpoint).GetMethod("HandleAsync");
        _methodResolverMock
            .Setup(r => r.ResolveHandleMethod(typeof(TestConnectionEndpoint)))
            .Returns(handleMethod);

        // Act
        await _invoker.InvokeEndpointAsync(endpoint, context, CancellationToken.None);

        // Assert
        Assert.True(endpoint.WasInvoked);
        //_loggerMock.VerifyLogInvokingEndpoint("TestConnectionEndpoint", "conn-123", Times.Once());
        //_loggerMock.VerifyLogInvocationSuccess("TestConnectionEndpoint", "conn-123", Times.Once());
        //_loggerMock.VerifyLogCacheMiss("TestConnectionEndpoint", Times.Once());
    }

    //[Fact]
    //public async Task InvokeEndpointAsync_WithRequestEndpoint_InvokesSuccessfully()
    //{
    //    // Arrange
    //    var endpoint = new TestRequestEndpoint();
    //    var context = new TestExecutionContext();
    //    var connectionMock = new Mock<IWsConnection>();
    //    connectionMock.Setup(c => c.Id).Returns("conn-456");
    //    context.Connection = connectionMock.Object;

    //    var request = new TestRequest { Message = "Test", Value = 42 };
    //    var serializerMock = new Mock<ISerializer>();
    //    serializerMock
    //        .Setup(s => s.Deserialize(It.IsAny<Type>(), It.IsAny<ReadOnlySpan<byte>>()))
    //        .Returns(request);
    //    context.Serializer = serializerMock.Object;

    //    var handleMethod = typeof(TestRequestEndpoint).GetMethod("HandleAsync");
    //    _methodResolverMock
    //        .Setup(r => r.ResolveHandleMethod(typeof(TestRequestEndpoint)))
    //        .Returns(handleMethod);

    //    // Act
    //    await _invoker.InvokeEndpointAsync(endpoint, context, CancellationToken.None);

    //    // Assert
    //    Assert.True(endpoint.WasInvoked);
    //    Assert.Same(request, endpoint.ReceivedRequest);
    //    _loggerMock.VerifyLogInvokingEndpoint("TestRequestEndpoint", "conn-456", Times.Once());
    //    _loggerMock.VerifyLogInvocationSuccess("TestRequestEndpoint", "conn-456", Times.Once());
    //}

    [Fact]
    public async Task InvokeEndpointAsync_WithCachedInvoker_UsesCache()
    {
        // Arrange
        var endpoint1 = new TestConnectionEndpoint();
        var endpoint2 = new TestConnectionEndpoint();
        var context = new TestExecutionContext();
        var connectionMock = new Mock<IWsConnection>();
        connectionMock.Setup(c => c.Id).Returns("conn-789");
        context.Connection = connectionMock.Object;

        var handleMethod = typeof(TestConnectionEndpoint).GetMethod("HandleAsync");
        _methodResolverMock
            .Setup(r => r.ResolveHandleMethod(typeof(TestConnectionEndpoint)))
            .Returns(handleMethod);

        // Act - First invocation (cache miss)
        await _invoker.InvokeEndpointAsync(endpoint1, context, CancellationToken.None);

        // Second invocation (cache hit)
        await _invoker.InvokeEndpointAsync(endpoint2, context, CancellationToken.None);

        // Assert
        Assert.True(endpoint1.WasInvoked);
        Assert.True(endpoint2.WasInvoked);
        //_loggerMock.VerifyLogCacheMiss("TestConnectionEndpoint", Times.Once());
        //_loggerMock.VerifyLogCacheHit("TestConnectionEndpoint", Times.Once());
        _methodResolverMock.Verify(r => r.ResolveHandleMethod(typeof(TestConnectionEndpoint)), Times.Once());
    }

    [Fact]
    public async Task InvokeEndpointAsync_WhenHandleMethodReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var endpointMock = new Mock<object>();
        var context = new TestExecutionContext();
        var connectionMock = new Mock<IWsConnection>();
        connectionMock.Setup(c => c.Id).Returns("conn-null");
        context.Connection = connectionMock.Object;

        var handleMethod = typeof(MockEndpointWithNullReturn).GetMethod("HandleAsync");
        _methodResolverMock
            .Setup(r => r.ResolveHandleMethod(It.IsAny<Type>()))
            .Returns(handleMethod);

        var endpoint = new MockEndpointWithNullReturn();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _invoker.InvokeEndpointAsync(endpoint, context, CancellationToken.None));

        Assert.Equal("HandleAsync method returned null", exception.Message);
        //_loggerMock.VerifyLogInvocationFailed("MockEndpointWithNullReturn", "conn-null", Times.Once());
    }

    [Fact]
    public async Task InvokeEndpointAsync_WhenMethodResolutionFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var endpoint = new TestConnectionEndpoint();
        var context = new TestExecutionContext();
        var connectionMock = new Mock<IWsConnection>();
        connectionMock.Setup(c => c.Id).Returns("conn-fail");
        context.Connection = connectionMock.Object;

        _methodResolverMock
            .Setup(r => r.ResolveHandleMethod(typeof(TestConnectionEndpoint)))
            .Returns((MethodInfo)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _invoker.InvokeEndpointAsync(endpoint, context, CancellationToken.None));

        Assert.Contains("Could not resolve handle method for endpoint type TestConnectionEndpoint", exception.Message);

        //_loggerMock.VerifyLogNullMethodResolution("TestConnectionEndpoint", Times.Once());
    }

    //[Fact]
    //public async Task InvokeEndpointAsync_WhenEndpointThrowsException_LogsAndRethrows()
    //{
    //    // Arrange
    //    var endpoint = new ThrowingEndpoint();
    //    var context = new TestExecutionContext();
    //    var connectionMock = new Mock<IWsConnection>();
    //    connectionMock.Setup(c => c.Id).Returns("conn-throw");
    //    context.Connection = connectionMock.Object;

    //    var handleMethod = typeof(ThrowingEndpoint).GetMethod("HandleAsync");
    //    _methodResolverMock
    //        .Setup(r => r.ResolveHandleMethod(typeof(ThrowingEndpoint)))
    //        .Returns(handleMethod);

    //    // Act & Assert
    //    await Assert.ThrowsAsync<InvalidOperationException>(() =>
    //        _invoker.InvokeEndpointAsync(endpoint, context, CancellationToken.None));

    //    //_loggerMock.VerifyLogInvocationFailed("ThrowingEndpoint", "conn-throw", Times.Once());
    //}

    [Fact]
    public void GetInvoker_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _invoker.GetInvoker(null!));
    }

    [Fact]
    public void GetInvoker_WithNewType_CreatesAndCachesInvoker()
    {
        // Arrange
        var handleMethod = typeof(TestConnectionEndpoint).GetMethod("HandleAsync");
        _methodResolverMock
            .Setup(r => r.ResolveHandleMethod(typeof(TestConnectionEndpoint)))
            .Returns(handleMethod);

        // Act
        var invoker1 = _invoker.GetInvoker(typeof(TestConnectionEndpoint));
        var invoker2 = _invoker.GetInvoker(typeof(TestConnectionEndpoint));

        // Assert
        Assert.NotNull(invoker1);
        Assert.Same(invoker1, invoker2);
        _methodResolverMock.Verify(r => r.ResolveHandleMethod(typeof(TestConnectionEndpoint)), Times.Once());
    }

    [Fact]
    public async Task InvokeEndpointAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var endpoint = new CancellableEndpoint();
        var cts = new CancellationTokenSource();
        var context = new TestExecutionContext { Cancellation = cts.Token };

        var handleMethod = typeof(CancellableEndpoint).GetMethod("HandleAsync");
        _methodResolverMock
            .Setup(r => r.ResolveHandleMethod(typeof(CancellableEndpoint)))
            .Returns(handleMethod);

        // Act
        await _invoker.InvokeEndpointAsync(endpoint, context, cts.Token);

        // Assert
        Assert.True(endpoint.WasInvoked);
        Assert.True(endpoint.ReceivedCancellationToken == cts.Token);
    }

    [Fact]
    public async Task InvokeEndpointAsync_ConcurrentAccess_CreatesInvokerOnlyOnce()
    {
        // Arrange
        var endpointType = typeof(TestConnectionEndpoint);
        var handleMethod = typeof(TestConnectionEndpoint).GetMethod("HandleAsync");
        var context = new TestExecutionContext();

        _methodResolverMock
            .Setup(r => r.ResolveHandleMethod(endpointType))
            .Returns(handleMethod);

        var invoker = new EndpointInvoker(_methodResolverMock.Object, _loggerMock.Object);
        var tasks = new List<Task>();
        var endpointInstances = Enumerable.Range(0, 10)
            .Select(_ => new TestConnectionEndpoint())
            .ToArray();

        // Act
        foreach (var endpoint in endpointInstances)
        {
            tasks.Add(invoker.InvokeEndpointAsync(endpoint, context, CancellationToken.None));
        }

        await Task.WhenAll(tasks);

        // Assert
        _methodResolverMock.Verify(r => r.ResolveHandleMethod(endpointType), Times.Once());
        Assert.All(endpointInstances, e => Assert.True(e.WasInvoked));
    }

    // Test endpoint classes
    public class MockEndpointWithNullReturn
    {
        public Task? HandleAsync(IWsExecutionContext ctx, CancellationToken ct)
        {
            return null; // This should cause an exception
        }
    }

    public class ThrowingEndpoint
    {
        public Task HandleAsync(IWsExecutionContext ctx, CancellationToken ct)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    public class CancellableEndpoint
    {
        public bool WasInvoked { get; set; }
        public CancellationToken? ReceivedCancellationToken { get; set; }

        public Task HandleAsync(IWsExecutionContext ctx, CancellationToken ct)
        {
            WasInvoked = true;
            ReceivedCancellationToken = ct;
            return Task.CompletedTask;
        }
    }
}

// Extension method for testing (you'll need to implement this in your actual code)
public static class TypeExtensions
{
    public static bool AcceptsRequestMessages(this Type endpointType)
    {
        // This is a simplified implementation for testing
        // In real code, this would contain your actual logic
        return endpointType.GetMethods()
            .Any(m => m.Name == "HandleAsync" && m.GetParameters().Length == 3);
    }

    public static Type GetRequestType(this Type endpointType)
    {
        // This is a simplified implementation for testing
        var handleMethod = endpointType.GetMethods()
            .First(m => m.Name == "HandleAsync" && m.GetParameters().Length == 3);

        return handleMethod.GetParameters()[0].ParameterType;
    }
}
