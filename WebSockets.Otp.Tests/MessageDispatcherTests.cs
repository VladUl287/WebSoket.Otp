//using Moq;
//using Xunit;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using WebSockets.Otp.Abstractions.Contracts;
//using WebSockets.Otp.Core;

//namespace WebSockets.Otp.Tests;

//public class MessageDispatcherTests
//{
//    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
//    private readonly Mock<IWsEndpointRegistry> _endpointRegistryMock;
//    private readonly Mock<IExecutionContextFactory> _contextFactoryMock;
//    private readonly Mock<IEndpointInvoker> _invokerMock;
//    private readonly Mock<ILogger<MessageDispatcher>> _loggerMock;
//    private readonly MessageDispatcher _dispatcher;

//    // Mocks for dependencies used during execution
//    private readonly Mock<IWsConnection> _connectionMock;
//    private readonly Mock<ISerializer> _serializerMock;
//    private readonly Mock<IMessageBuffer> _bufferMock;
//    private readonly Mock<IServiceScope> _serviceScopeMock;
//    private readonly Mock<IServiceProvider> _serviceProviderMock;

//    public MessageDispatcherTests()
//    {
//        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
//        _endpointRegistryMock = new Mock<IWsEndpointRegistry>();
//        _contextFactoryMock = new Mock<IExecutionContextFactory>();
//        _invokerMock = new Mock<IEndpointInvoker>();
//        _loggerMock = new Mock<ILogger<MessageDispatcher>>();
//        _connectionMock = new Mock<IWsConnection>();
//        _serializerMock = new Mock<ISerializer>();
//        _bufferMock = new Mock<IMessageBuffer>();
//        _serviceScopeMock = new Mock<IServiceScope>();
//        _serviceProviderMock = new Mock<IServiceProvider>();

//        _dispatcher = new MessageDispatcher(
//            _scopeFactoryMock.Object,
//            _endpointRegistryMock.Object,
//            _contextFactoryMock.Object,
//            _invokerMock.Object,
//            _loggerMock.Object
//        );

//        SetupCommonMocks();
//    }

//    private void SetupCommonMocks()
//    {
//        _connectionMock.Setup(c => c.Id).Returns("test-connection-id");
//        //_bufferMock.Setup(b => b.Span).Returns(new byte[] { 1, 2, 3, 4, 5 });
//        //_scopeFactoryMock.Setup(f => f.CreateAsyncScope()).Returns(_serviceScopeMock.Object);
//        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
//    }

//    [Fact]
//    public async Task DispatchMessage_ValidMessage_SuccessfullyDispatches()
//    {
//        // Arrange
//        var endpointKey = "test-endpoint";
//        var endpointType = typeof(TestEndpoint);
//        var endpointInstance = new TestEndpoint();
//        var cancellationToken = new CancellationToken();
//        var executionContext = new Mock<IWsExecutionContext>().Object;

//        //_serializerMock
//        //    .Setup(s => s.ExtractStringField("key", It.IsAny<ReadOnlySpan<byte>>()))
//        //    .Returns(endpointKey);

//        _endpointRegistryMock
//            .Setup(r => r.Resolve(endpointKey))
//            .Returns(endpointType);

//        _serviceProviderMock
//            .Setup(p => p.GetService(endpointType))
//            .Returns(endpointInstance);

//        _contextFactoryMock
//            .Setup(f => f.Create(endpointKey, endpointType, _connectionMock.Object, _bufferMock.Object, _serializerMock.Object, cancellationToken))
//            .Returns(executionContext);

//        _invokerMock
//            .Setup(i => i.InvokeEndpointAsync(endpointInstance, executionContext, cancellationToken))
//            .Returns(Task.CompletedTask);

//        // Act
//        await _dispatcher.DispatchMessage(_connectionMock.Object, _serializerMock.Object, _bufferMock.Object, cancellationToken);

//        // Assert
//        _serializerMock.Verify(s => s.ExtractStringField("key", It.IsAny<ReadOnlySpan<byte>>()), Times.Once);
//        _endpointRegistryMock.Verify(r => r.Resolve(endpointKey), Times.Once);
//        _serviceProviderMock.Verify(p => p.GetService(endpointType), Times.Once);
//        _contextFactoryMock.Verify(f => f.Create(endpointKey, endpointType, _connectionMock.Object, _bufferMock.Object, _serializerMock.Object, cancellationToken), Times.Once);
//        _invokerMock.Verify(i => i.InvokeEndpointAsync(endpointInstance, executionContext, cancellationToken), Times.Once);

//        VerifyLogMessageDispatched("test-connection-id", endpointKey);
//    }

//    [Fact]
//    public async Task DispatchMessage_KeyExtractionFails_ThrowsMessageFormatException()
//    {
//        // Arrange
//        _serializerMock
//            .Setup(s => s.ExtractStringField("key", It.IsAny<ReadOnlySpan<byte>>()))
//            .Returns((string)null);

//        // Act & Assert
//        var exception = await Assert.ThrowsAsync<MessageFormatException>(() =>
//            _dispatcher.DispatchMessage(_connectionMock.Object, _serializerMock.Object, _bufferMock.Object, CancellationToken.None));

//        Assert.Equal("Unable to determine message route from payload", exception.Message);
//        VerifyLogKeyExtractionFailed("test-connection-id");
//    }

//    [Fact]
//    public async Task DispatchMessage_EndpointNotFound_ThrowsEndpointNotFoundException()
//    {
//        // Arrange
//        var endpointKey = "non-existent-endpoint";

//        _serializerMock
//            .Setup(s => s.ExtractStringField("key", It.IsAny<ReadOnlySpan<byte>>()))
//            .Returns(endpointKey);

//        _endpointRegistryMock
//            .Setup(r => r.Resolve(endpointKey))
//            .Returns((Type)null);

//        // Act & Assert
//        var exception = await Assert.ThrowsAsync<EndpointNotFoundException>(() =>
//            _dispatcher.DispatchMessage(_connectionMock.Object, _serializerMock.Object, _bufferMock.Object, CancellationToken.None));

//        Assert.Equal($"Endpoint for route '{endpointKey}' not found", exception.Message);
//        VerifyLogEndpointNotFound("test-connection-id", endpointKey);
//    }

//    [Fact]
//    public async Task DispatchMessage_EndpointServiceNotRegistered_ThrowsEndpointNotFoundException()
//    {
//        // Arrange
//        var endpointKey = "test-endpoint";
//        var endpointType = typeof(TestEndpoint);

//        _serializerMock
//            .Setup(s => s.ExtractStringField("key", It.IsAny<ReadOnlySpan<byte>>()))
//            .Returns(endpointKey);

//        _endpointRegistryMock
//            .Setup(r => r.Resolve(endpointKey))
//            .Returns(endpointType);

//        _serviceProviderMock
//            .Setup(p => p.GetService(endpointType))
//            .Returns(null);

//        // Act & Assert
//        var exception = await Assert.ThrowsAsync<EndpointNotFoundException>(() =>
//            _dispatcher.DispatchMessage(_connectionMock.Object, _serializerMock.Object, _bufferMock.Object, CancellationToken.None));

//        Assert.Equal($"Endoind with type '{endpointType}' not found", exception.Message);
//        VerifyLogEndpointServiceNotFound("test-connection-id", endpointType.Name);
//    }

//    [Fact]
//    public async Task DispatchMessage_ScopeIsDisposedEvenIfInvocationFails()
//    {
//        // Arrange
//        var endpointKey = "test-endpoint";
//        var endpointType = typeof(TestEndpoint);
//        var endpointInstance = new TestEndpoint();
//        var expectedException = new InvalidOperationException("Invocation failed");

//        _serializerMock
//            .Setup(s => s.ExtractStringField("key", It.IsAny<ReadOnlySpan<byte>>()))
//            .Returns(endpointKey);

//        _endpointRegistryMock
//            .Setup(r => r.Resolve(endpointKey))
//            .Returns(endpointType);

//        _serviceProviderMock
//            .Setup(p => p.GetService(endpointType))
//            .Returns(endpointInstance);

//        _contextFactoryMock
//            .Setup(f => f.Create(It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<IWsConnection>(), It.IsAny<IMessageBuffer>(), It.IsAny<ISerializer>(), It.IsAny<CancellationToken>()))
//            .Returns(Mock.Of<IExecutionContext>());

//        _invokerMock
//            .Setup(i => i.InvokeEndpointAsync(It.IsAny<object>(), It.IsAny<IExecutionContext>(), It.IsAny<CancellationToken>()))
//            .ThrowsAsync(expectedException);

//        // Act & Assert
//        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
//            _dispatcher.DispatchMessage(_connectionMock.Object, _serializerMock.Object, _bufferMock.Object, CancellationToken.None));

//        Assert.Equal(expectedException, exception);
//        _serviceScopeMock.Verify(s => s.DisposeAsync(), Times.Once);
//    }

//    [Fact]
//    public async Task DispatchMessage_CancellationTokenPropagated()
//    {
//        // Arrange
//        var endpointKey = "test-endpoint";
//        var endpointType = typeof(TestEndpoint);
//        var endpointInstance = new TestEndpoint();
//        var cancellationToken = new CancellationToken(true); // Already canceled
//        var executionContext = new Mock<IExecutionContext>().Object;

//        _serializerMock
//            .Setup(s => s.ExtractStringField("key", It.IsAny<ReadOnlySpan<byte>>()))
//            .Returns(endpointKey);

//        _endpointRegistryMock
//            .Setup(r => r.Resolve(endpointKey))
//            .Returns(endpointType);

//        _serviceProviderMock
//            .Setup(p => p.GetService(endpointType))
//            .Returns(endpointInstance);

//        _contextFactoryMock
//            .Setup(f => f.Create(endpointKey, endpointType, _connectionMock.Object, _bufferMock.Object, _serializerMock.Object, cancellationToken))
//            .Returns(executionContext);

//        _invokerMock
//            .Setup(i => i.InvokeEndpointAsync(endpointInstance, executionContext, cancellationToken))
//            .ThrowsAsync(new TaskCanceledException());

//        // Act & Assert
//        await Assert.ThrowsAsync<TaskCanceledException>(() =>
//            _dispatcher.DispatchMessage(_connectionMock.Object, _serializerMock.Object, _bufferMock.Object, cancellationToken));

//        _contextFactoryMock.Verify(f => f.Create(
//            It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<IWsConnection>(), It.IsAny<IMessageBuffer>(),
//            It.IsAny<ISerializer>(), cancellationToken), Times.Once);
//    }

//    [Fact]
//    public async Task DispatchMessage_LogsAppropriateMessagesThroughoutProcess()
//    {
//        // Arrange
//        var endpointKey = "test-endpoint";
//        var endpointType = typeof(TestEndpoint);
//        var endpointInstance = new TestEndpoint();
//        var cancellationToken = CancellationToken.None;

//        _serializerMock
//            .Setup(s => s.ExtractStringField("key", It.IsAny<ReadOnlySpan<byte>>()))
//            .Returns(endpointKey);

//        _endpointRegistryMock
//            .Setup(r => r.Resolve(endpointKey))
//            .Returns(endpointType);

//        _serviceProviderMock
//            .Setup(p => p.GetService(endpointType))
//            .Returns(endpointInstance);

//        _contextFactoryMock
//            .Setup(f => f.Create(It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<IWsConnection>(), It.IsAny<IMessageBuffer>(), It.IsAny<ISerializer>(), It.IsAny<CancellationToken>()))
//            .Returns(Mock.Of<IExecutionContext>());

//        // Act
//        await _dispatcher.DispatchMessage(_connectionMock.Object, _serializerMock.Object, _bufferMock.Object, cancellationToken);

//        // Assert - Verify all expected log calls were made
//        VerifyLogDispatchingMessage("test-connection-id", "Unknown", 5);
//        VerifyLogEndpointResolved("test-connection-id", endpointType.Name, It.IsAny<bool>());
//        VerifyLogMessageDispatched("test-connection-id", endpointKey);
//    }

//    [Theory]
//    [InlineData("TestEndpoint")]
//    [InlineData("AnotherEndpoint")]
//    [InlineData("CustomEndpoint")]
//    public async Task DispatchMessage_WithDifferentEndpointKeys_ResolvesAppropriateTypes(string endpointKey)
//    {
//        // Arrange
//        var endpointType = typeof(TestEndpoint);
//        var endpointInstance = new TestEndpoint();

//        _serializerMock
//            .Setup(s => s.ExtractStringField("key", It.IsAny<ReadOnlySpan<byte>>()))
//            .Returns(endpointKey);

//        _endpointRegistryMock
//            .Setup(r => r.Resolve(endpointKey))
//            .Returns(endpointType);

//        _serviceProviderMock
//            .Setup(p => p.GetService(endpointType))
//            .Returns(endpointInstance);

//        _contextFactoryMock
//            .Setup(f => f.Create(It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<IWsConnection>(), It.IsAny<IMessageBuffer>(), It.IsAny<ISerializer>(), It.IsAny<CancellationToken>()))
//            .Returns(Mock.Of<IExecutionContext>());

//        // Act
//        await _dispatcher.DispatchMessage(_connectionMock.Object, _serializerMock.Object, _bufferMock.Object, CancellationToken.None);

//        // Assert
//        _endpointRegistryMock.Verify(r => r.Resolve(endpointKey), Times.Once);
//        _contextFactoryMock.Verify(f => f.Create(endpointKey, endpointType, It.IsAny<IWsConnection>(), It.IsAny<IMessageBuffer>(), It.IsAny<ISerializer>(), It.IsAny<CancellationToken>()), Times.Once);
//    }

//    // Helper methods for verifying log calls
//    private void VerifyLogDispatchingMessage(string connectionId, string endpointKey, int payloadLength)
//    {
//        _loggerMock.Verify(logger => logger.Log(
//            It.Is<LogLevel>(level => level == LogLevel.Information),
//            It.IsAny<EventId>(),
//            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Dispatching message for connection {connectionId}")),
//            It.IsAny<Exception>(),
//            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
//            Times.Once);
//    }

//    private void VerifyLogKeyExtractionFailed(string connectionId)
//    {
//        _loggerMock.Verify(logger => logger.Log(
//            It.Is<LogLevel>(level => level == LogLevel.Warning),
//            It.IsAny<EventId>(),
//            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Key extraction failed for connection {connectionId}")),
//            It.IsAny<Exception>(),
//            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
//            Times.Once);
//    }

//    private void VerifyLogEndpointNotFound(string connectionId, string endpointKey)
//    {
//        _loggerMock.Verify(logger => logger.Log(
//            It.Is<LogLevel>(level => level == LogLevel.Warning),
//            It.IsAny<EventId>(),
//            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Endpoint not found for connection {connectionId}")),
//            It.IsAny<Exception>(),
//            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
//            Times.Once);
//    }

//    private void VerifyLogEndpointServiceNotFound(string connectionId, string endpointTypeName)
//    {
//        _loggerMock.Verify(logger => logger.Log(
//            It.Is<LogLevel>(level => level == LogLevel.Warning),
//            It.IsAny<EventId>(),
//            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Endpoint service not found for connection {connectionId}")),
//            It.IsAny<Exception>(),
//            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
//            Times.Once);
//    }

//    private void VerifyLogEndpointResolved(string connectionId, string endpointTypeName, bool acceptsRequests)
//    {
//        _loggerMock.Verify(logger => logger.Log(
//            It.Is<LogLevel>(level => level == LogLevel.Debug),
//            It.IsAny<EventId>(),
//            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Endpoint resolved for connection {connectionId}")),
//            It.IsAny<Exception>(),
//            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
//            Times.Once);
//    }

//    private void VerifyLogMessageDispatched(string connectionId, string endpointKey)
//    {
//        _loggerMock.Verify(logger => logger.Log(
//            It.Is<LogLevel>(level => level == LogLevel.Information),
//            It.IsAny<EventId>(),
//            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Message dispatched for connection {connectionId}")),
//            It.IsAny<Exception>(),
//            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
//            Times.Once);
//    }

//    // Test endpoint class
//    private class TestEndpoint { }
//}

//// Extension methods for logging (these would typically be in the same namespace as your logger calls)
//public static class LoggerExtensions
//{
//    public static void LogDispatchingMessage(this ILogger logger, string connectionId, string endpointKey, int payloadLength) { }
//    public static void LogKeyExtractionFailed(this ILogger logger, string connectionId) { }
//    public static void LogEndpointNotFound(this ILogger logger, string connectionId, string endpointKey) { }
//    public static void LogEndpointServiceNotFound(this ILogger logger, string connectionId, string endpointTypeName) { }
//    public static void LogEndpointResolved(this ILogger logger, string connectionId, string endpointTypeName, bool acceptsRequests) { }
//    public static void LogMessageDispatched(this ILogger logger, string connectionId, string endpointKey) { }
//}

//// Custom exceptions
//public class MessageFormatException : Exception
//{
//    public MessageFormatException(string message) : base(message) { }
//}

//public class EndpointNotFoundException : Exception
//{
//    public EndpointNotFoundException(string message) : base(message) { }
//}