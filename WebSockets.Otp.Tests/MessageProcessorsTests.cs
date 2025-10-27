using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Processors;
using Xunit;

namespace WebSockets.Otp.Tests;

public class ParallelMessageProcessorTests
{
    private readonly Mock<IMessageDispatcher> _mockDispatcher;
    private readonly Mock<IMessageBufferFactory> _mockBufferFactory;
    private readonly Mock<ISerializerFactory> _mockSerializerFactory;
    private readonly Mock<ILogger<SequentialMessageProcessor>> _mockLogger;
    private readonly ParallelMessageProcessor _processor;

    public ParallelMessageProcessorTests()
    {
        _mockDispatcher = new Mock<IMessageDispatcher>();
        _mockBufferFactory = new Mock<IMessageBufferFactory>();
        _mockSerializerFactory = new Mock<ISerializerFactory>();
        _mockLogger = new Mock<ILogger<SequentialMessageProcessor>>();

        _processor = new ParallelMessageProcessor(
            _mockDispatcher.Object,
            _mockBufferFactory.Object,
            _mockSerializerFactory.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Name_ShouldReturnParallel()
    {
        // Assert
        Assert.Equal(ProcessingMode.Parallel, _processor.Name);
    }

    [Fact]
    public async Task Process_WhenConnectionIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _processor.Process(null, new WsMiddlewareOptions()));
    }

    [Fact]
    public async Task Process_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _processor.Process(mockConnection.Object, null));
    }

    [Fact]
    public async Task Process_WhenWebSocketCloses_ShouldStopProcessing()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(WebSocketState.Closed).Object);
        var options = CreateDefaultOptions();

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        _mockDispatcher.Verify(d => d.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ISerializer>(), It.IsAny<IMessageBuffer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenCancellationRequested_ShouldStopProcessing()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockConnection = CreateMockConnection(cts.Token);
        var options = CreateDefaultOptions();

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        _mockDispatcher.Verify(d => d.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ISerializer>(), It.IsAny<IMessageBuffer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenMessageTooBig_ShouldCloseConnection()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(
            WebSocketState.Open,
            new List<WebSocketReceiveResult>
            {
                new WebSocketReceiveResult(1024, WebSocketMessageType.Text, false),
                new WebSocketReceiveResult(1024 * 1024, WebSocketMessageType.Text, true) // Large message
            }).Object);

        var options = CreateDefaultOptions();
        options.Memory.MaxMessageSize = 1024; // Small limit

        var mockBuffer = new Mock<IMessageBuffer>();
        mockBuffer.SetupGet(b => b.Length).Returns(900); // Already close to limit
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        mockConnection.Verify(c => c.Socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message exceeds size limit", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Process_WhenCloseMessageReceived_ShouldCloseConnection()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(
            WebSocketState.Open,
            new List<WebSocketReceiveResult>
            {
                new WebSocketReceiveResult(0, WebSocketMessageType.Close, true)
            }).Object);

        var options = CreateDefaultOptions();

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        mockConnection.Verify(c => c.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Process_WithMultipleMessages_ShouldProcessInParallel()
    {
        // Arrange
        var messageCount = 5;
        var processedMessages = 0;
        var processingTasks = new List<Task>();
        var processingLock = new object();

        var mockConnection = CreateMockConnection();
        var receiveResults = new List<WebSocketReceiveResult>();

        for (int i = 0; i < messageCount; i++)
        {
            receiveResults.Add(new WebSocketReceiveResult(100, WebSocketMessageType.Text, true));
        }

        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(WebSocketState.Open, receiveResults).Object);

        _mockDispatcher.Setup(d => d.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ISerializer>(), It.IsAny<IMessageBuffer>(), It.IsAny<CancellationToken>()))
            .Returns(async (IWsConnection conn, ISerializer serializer, IMessageBuffer buffer, CancellationToken token) =>
            {
                lock (processingLock)
                {
                    processedMessages++;
                }
                await Task.Delay(100, token); // Simulate processing time
            });

        var options = CreateDefaultOptions();
        options.Processing.MaxParallelOperations = 3;

        var mockBuffer = new Mock<IMessageBuffer>();
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        Assert.Equal(messageCount, processedMessages);
        _mockDispatcher.Verify(d => d.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ISerializer>(), It.IsAny<IMessageBuffer>(), It.IsAny<CancellationToken>()), Times.Exactly(messageCount));
    }

    [Fact]
    public async Task Process_WhenReclaimBuffersImmediately_ShouldShrinkBuffer()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(
            WebSocketState.Open,
            new List<WebSocketReceiveResult>
            {
                new WebSocketReceiveResult(100, WebSocketMessageType.Text, true)
            }).Object);

        var options = CreateDefaultOptions();
        options.Memory.ReclaimBuffersImmediately = true;

        var mockBuffer = new Mock<IMessageBuffer>();
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        mockBuffer.Verify(b => b.Shrink(), Times.Once);
    }

    [Fact]
    public async Task Process_WhenDispatcherThrowsException_ShouldContinueProcessingOtherMessages()
    {
        // Arrange
        var messageCount = 3;
        var processedMessages = 0;

        var mockConnection = CreateMockConnection();
        var receiveResults = new List<WebSocketReceiveResult>();

        for (int i = 0; i < messageCount; i++)
        {
            receiveResults.Add(new WebSocketReceiveResult(100, WebSocketMessageType.Text, true));
        }

        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(WebSocketState.Open, receiveResults).Object);

        _mockDispatcher.SetupSequence(d => d.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ISerializer>(), It.IsAny<IMessageBuffer>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"))
            .Returns(Task.CompletedTask)
            .Returns(Task.CompletedTask);

        var options = CreateDefaultOptions();
        options.Processing.MaxParallelOperations = 2;

        var mockBuffer = new Mock<IMessageBuffer>();
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert - Should not throw and should process all messages
        _mockDispatcher.Verify(d => d.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ISerializer>(), It.IsAny<IMessageBuffer>(), It.IsAny<CancellationToken>()), Times.Exactly(messageCount));
    }

    private Mock<IWsConnection> CreateMockConnection(CancellationToken? cancellationToken = null)
    {
        var mockConnection = new Mock<IWsConnection>();
        var mockContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();

        var token = cancellationToken ?? new CancellationToken(false);
        mockRequest.SetupGet(r => r.HttpContext.RequestAborted).Returns(token);
        mockContext.SetupGet(c => c.Request).Returns(mockRequest.Object);

        mockConnection.SetupGet(c => c.Context).Returns(mockContext.Object);
        mockConnection.SetupGet(c => c.Id).Returns(Guid.NewGuid().ToString());

        return mockConnection;
    }

    private Mock<WebSocket> CreateMockWebSocket(WebSocketState state, List<WebSocketReceiveResult> receiveResults = null)
    {
        var mockSocket = new Mock<WebSocket>();
        mockSocket.SetupGet(s => s.State).Returns(state);

        if (receiveResults != null)
        {
            var queue = new Queue<WebSocketReceiveResult>(receiveResults);
            mockSocket.Setup(s => s.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(() => queue.Count > 0 ? queue.Dequeue() : new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        }

        return mockSocket;
    }

    private WsMiddlewareOptions CreateDefaultOptions()
    {
        return new WsMiddlewareOptions
        {
            Memory = new MemorySettings
            {
                MaxBufferPoolSize = 10,
                InitialBufferSize = 1024,
                MaxMessageSize = 1024 * 1024,
                ReclaimBuffersImmediately = false
            },
            Processing = new ProcessingSettings
            {
                MaxParallelOperations = 4
            },
            Connection = new ConnectionSettings
            {
                Protocol = "json"
            }
        };
    }
}

public class SequentialMessageProcessorTests
{
    private readonly Mock<IMessageDispatcher> _mockDispatcher;
    private readonly Mock<IMessageBufferFactory> _mockBufferFactory;
    private readonly Mock<ISerializerFactory> _mockSerializerFactory;
    private readonly Mock<ILogger<SequentialMessageProcessor>> _mockLogger;
    private readonly SequentialMessageProcessor _processor;

    public SequentialMessageProcessorTests()
    {
        _mockDispatcher = new Mock<IMessageDispatcher>();
        _mockBufferFactory = new Mock<IMessageBufferFactory>();
        _mockSerializerFactory = new Mock<ISerializerFactory>();
        _mockLogger = new Mock<ILogger<SequentialMessageProcessor>>();

        _processor = new SequentialMessageProcessor(
            _mockDispatcher.Object,
            _mockBufferFactory.Object,
            _mockSerializerFactory.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Name_ShouldReturnSequential()
    {
        // Assert
        Assert.Equal(ProcessingMode.Sequential, _processor.Name);
    }

    [Fact]
    public async Task Process_WhenConnectionIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _processor.Process(null, new WsMiddlewareOptions()));
    }

    [Fact]
    public async Task Process_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _processor.Process(mockConnection.Object, null));
    }

    [Fact]
    public async Task Process_WithSingleCompleteMessage_ShouldDispatchOnce()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(
            WebSocketState.Open,
            new List<WebSocketReceiveResult>
            {
                new WebSocketReceiveResult(100, WebSocketMessageType.Text, true)
            }).Object);

        var options = CreateDefaultOptions();
        var mockBuffer = new Mock<IMessageBuffer>();
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        var mockSerializer = new Mock<ISerializer>();
        _mockSerializerFactory.Setup(f => f.Resolve(It.IsAny<string>())).Returns(mockSerializer.Object);

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        _mockDispatcher.Verify(d => d.DispatchMessage(
            mockConnection.Object,
            mockSerializer.Object,
            mockBuffer.Object,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Process_WithFragmentedMessage_ShouldDispatchWhenComplete()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(
            WebSocketState.Open,
            new List<WebSocketReceiveResult>
            {
                new WebSocketReceiveResult(100, WebSocketMessageType.Text, false),
                new WebSocketReceiveResult(100, WebSocketMessageType.Text, false),
                new WebSocketReceiveResult(100, WebSocketMessageType.Text, true) // End of message
            }).Object);

        var options = CreateDefaultOptions();
        var mockBuffer = new Mock<IMessageBuffer>();
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        _mockDispatcher.Verify(d => d.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ISerializer>(), It.IsAny<IMessageBuffer>(), It.IsAny<CancellationToken>()), Times.Once);
        //mockBuffer.Verify(b => b.Write(It.IsAny<ReadOnlySpan<byte>>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Process_WhenMessageTooBig_ShouldCloseConnection()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(
            WebSocketState.Open,
            new List<WebSocketReceiveResult>
            {
                new WebSocketReceiveResult(1024, WebSocketMessageType.Text, false),
                new WebSocketReceiveResult(1024 * 1024, WebSocketMessageType.Text, true) // Large message
            }).Object);

        var options = CreateDefaultOptions();
        options.Memory.MaxMessageSize = 1024; // Small limit

        var mockBuffer = new Mock<IMessageBuffer>();
        mockBuffer.SetupGet(b => b.Length).Returns(900); // Already close to limit
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        mockConnection.Verify(c => c.Socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message exceeds size limit", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Process_WhenCloseMessageReceived_ShouldCloseConnection()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(
            WebSocketState.Open,
            new List<WebSocketReceiveResult>
            {
                new WebSocketReceiveResult(0, WebSocketMessageType.Close, true)
            }).Object);

        var options = CreateDefaultOptions();
        var mockBuffer = new Mock<IMessageBuffer>();
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        mockConnection.Verify(c => c.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Process_WhenCancellationRequested_ShouldStopProcessing()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockConnection = CreateMockConnection(cts.Token);
        var options = CreateDefaultOptions();
        var mockBuffer = new Mock<IMessageBuffer>();
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        _mockDispatcher.Verify(d => d.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ISerializer>(), It.IsAny<IMessageBuffer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenDispatcherThrowsException_ShouldLogAndRethrow()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(
            WebSocketState.Open,
            new List<WebSocketReceiveResult>
            {
                new WebSocketReceiveResult(100, WebSocketMessageType.Text, true)
            }).Object);

        var expectedException = new InvalidOperationException("Test exception");
        _mockDispatcher.Setup(d => d.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ISerializer>(), It.IsAny<IMessageBuffer>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var options = CreateDefaultOptions();
        var mockBuffer = new Mock<IMessageBuffer>();
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _processor.Process(mockConnection.Object, options));

        Assert.Equal(expectedException, exception);
    }

    [Fact]
    public async Task Process_WhenReclaimBuffersImmediately_ShouldShrinkBuffer()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(
            WebSocketState.Open,
            new List<WebSocketReceiveResult>
            {
                new WebSocketReceiveResult(100, WebSocketMessageType.Text, true)
            }).Object);

        var options = CreateDefaultOptions();
        options.Memory.ReclaimBuffersImmediately = true;

        var mockBuffer = new Mock<IMessageBuffer>();
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        mockBuffer.Verify(b => b.Shrink(), Times.Once);
    }

    [Fact]
    public async Task Process_WithMultipleCompleteMessages_ShouldDispatchEachMessage()
    {
        // Arrange
        var messageCount = 3;
        var mockConnection = CreateMockConnection();
        var receiveResults = new List<WebSocketReceiveResult>();

        for (int i = 0; i < messageCount; i++)
        {
            receiveResults.Add(new WebSocketReceiveResult(100, WebSocketMessageType.Text, true));
        }

        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(WebSocketState.Open, receiveResults).Object);

        var options = CreateDefaultOptions();
        var mockBuffer = new Mock<IMessageBuffer>();
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act
        await _processor.Process(mockConnection.Object, options);

        // Assert
        _mockDispatcher.Verify(d => d.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ISerializer>(), It.IsAny<IMessageBuffer>(), It.IsAny<CancellationToken>()), Times.Exactly(messageCount));
        mockBuffer.Verify(b => b.SetLength(0), Times.Exactly(messageCount));
    }

    [Fact]
    public async Task Process_ResourcesAreDisposed_EvenOnException()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        var mockSocket = mockConnection.SetupGet(c => c.Socket).Returns(CreateMockWebSocket(
            WebSocketState.Open,
            new List<WebSocketReceiveResult>
            {
                new WebSocketReceiveResult(100, WebSocketMessageType.Text, true)
            }).Object);

        _mockDispatcher.Setup(d => d.DispatchMessage(It.IsAny<IWsConnection>(), It.IsAny<ISerializer>(), It.IsAny<IMessageBuffer>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var options = CreateDefaultOptions();
        var mockBuffer = new Mock<IMessageBuffer>();
        _mockBufferFactory.Setup(f => f.Create(It.IsAny<int>())).Returns(mockBuffer.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _processor.Process(mockConnection.Object, options));

        // Verify resources are disposed
        mockBuffer.Verify(b => b.Dispose(), Times.Once);
    }

    private Mock<IWsConnection> CreateMockConnection(CancellationToken? cancellationToken = null)
    {
        var mockConnection = new Mock<IWsConnection>();
        var mockContext = new Mock<HttpContext>();

        var token = cancellationToken ?? new CancellationToken(false);
        mockContext.SetupGet(c => c.RequestAborted).Returns(token);

        mockConnection.SetupGet(c => c.Context).Returns(mockContext.Object);
        mockConnection.SetupGet(c => c.Id).Returns(Guid.NewGuid().ToString());

        return mockConnection;
    }

    private Mock<WebSocket> CreateMockWebSocket(WebSocketState state, List<WebSocketReceiveResult> receiveResults = null)
    {
        var mockSocket = new Mock<WebSocket>();
        mockSocket.SetupGet(s => s.State).Returns(state);

        if (receiveResults != null)
        {
            var queue = new Queue<WebSocketReceiveResult>(receiveResults);
            mockSocket.Setup(s => s.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(() => queue.Count > 0 ? queue.Dequeue() : new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        }

        return mockSocket;
    }

    private WsMiddlewareOptions CreateDefaultOptions()
    {
        return new WsMiddlewareOptions
        {
            Memory = new MemorySettings
            {
                InitialBufferSize = 1024,
                MaxMessageSize = 1024 * 1024,
                ReclaimBuffersImmediately = false
            },
            Connection = new ConnectionSettings
            {
                Protocol = "json"
            }
        };
    }
}
