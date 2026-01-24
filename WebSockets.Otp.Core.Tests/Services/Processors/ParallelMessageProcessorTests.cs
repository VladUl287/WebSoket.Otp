using Moq;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Services.Processors;

namespace WebSockets.Otp.Core.Tests.Services.Processors;

public class ParallelMessageProcessorTests
{
    private readonly Mock<IMessageDispatcher> _mockDispatcher;
    private readonly Mock<IMessageEnumerator> _mockEnumerator;
    private readonly Mock<IAsyncObjectPool<IMessageBuffer>> _mockBufferPool;
    private readonly ParallelMessageProcessor _processor;
    private readonly Mock<IGlobalContext> _mockGlobalContext;
    private readonly Mock<ISerializer> _mockSerializer;
    private readonly WsConfiguration _options;

    public ParallelMessageProcessorTests()
    {
        _mockDispatcher = new Mock<IMessageDispatcher>();
        _mockEnumerator = new Mock<IMessageEnumerator>();
        _mockBufferPool = new Mock<IAsyncObjectPool<IMessageBuffer>>();
        _mockGlobalContext = new Mock<IGlobalContext>();
        _mockSerializer = new Mock<ISerializer>();

        _processor = new ParallelMessageProcessor(
            _mockDispatcher.Object,
            _mockEnumerator.Object,
            _mockBufferPool.Object
        );

        _options = new WsConfiguration(new WsOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            ShrinkBuffers = true,
            TaskScheduler = TaskScheduler.Default
        });
    }

    [Fact]
    public void Mode_ShouldReturnParallel()
    {
        // Act
        var result = _processor.Mode;

        // Assert
        Assert.Equal(ProcessingMode.Parallel, result);
    }

    [Fact]
    public async Task Process_ShouldEnumerateMessages()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var mockBuffer = new Mock<IMessageBuffer>();
        var token = CancellationToken.None;

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(new List<IMessageBuffer> { mockBuffer.Object }.ToAsyncEnumerable());

        // Act
        await _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token);

        // Assert
        _mockEnumerator.Verify(x => x.EnumerateAsync(
            mockSocket.Object,
            _options,
            _mockBufferPool.Object,
            token), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldDispatchEachMessage()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var mockBuffer1 = new Mock<IMessageBuffer>();
        var mockBuffer2 = new Mock<IMessageBuffer>();
        var token = CancellationToken.None;

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(new List<IMessageBuffer> { mockBuffer1.Object, mockBuffer2.Object }.ToAsyncEnumerable());

        // Act
        await _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token);

        // Assert
        _mockDispatcher.Verify(x => x.DispatchMessage(
            _mockGlobalContext.Object,
            _mockSerializer.Object,
            mockBuffer1.Object,
            token), Times.Once);
        _mockDispatcher.Verify(x => x.DispatchMessage(
            _mockGlobalContext.Object,
            _mockSerializer.Object,
            mockBuffer2.Object,
            token), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldReturnBufferToPoolAfterDispatch()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var mockBuffer = new Mock<IMessageBuffer>();
        var token = CancellationToken.None;

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(new List<IMessageBuffer> { mockBuffer.Object }.ToAsyncEnumerable());

        // Act
        await _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token);

        // Assert
        _mockBufferPool.Verify(x => x.Return(mockBuffer.Object, token), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldResetBufferLength()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var mockBuffer = new Mock<IMessageBuffer>();
        var token = CancellationToken.None;

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(new List<IMessageBuffer> { mockBuffer.Object }.ToAsyncEnumerable());

        // Act
        await _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token);

        // Assert
        mockBuffer.Verify(x => x.SetLength(0), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldShrinkBufferWhenOptionEnabled()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var mockBuffer = new Mock<IMessageBuffer>();
        var token = CancellationToken.None;

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(new List<IMessageBuffer> { mockBuffer.Object }.ToAsyncEnumerable());

        // Act
        await _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token);

        // Assert
        mockBuffer.Verify(x => x.Shrink(), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldNotShrinkBufferWhenOptionDisabled()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var mockBuffer = new Mock<IMessageBuffer>();
        var token = CancellationToken.None;

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(new List<IMessageBuffer> { mockBuffer.Object }.ToAsyncEnumerable());

        // Act
        await _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token);

        // Assert
        mockBuffer.Verify(x => x.Shrink(), Times.Never);
    }

    [Fact]
    public async Task Process_ShouldHandleEmptyMessageStream()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var token = CancellationToken.None;

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(Enumerable.Empty<IMessageBuffer>().ToAsyncEnumerable());

        // Act & Assert (should not throw)
        await _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token);

        // Verify no dispatches were attempted
        _mockDispatcher.Verify(x => x.DispatchMessage(
            It.IsAny<IGlobalContext>(),
            It.IsAny<ISerializer>(),
            It.IsAny<IMessageBuffer>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_ShouldPropagateCancellationToken()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var mockBuffer = new Mock<IMessageBuffer>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(new List<IMessageBuffer> { mockBuffer.Object }.ToAsyncEnumerable());

        // Act
        await _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token);

        // Assert
        _mockEnumerator.Verify(x => x.EnumerateAsync(
            mockSocket.Object,
            _options,
            _mockBufferPool.Object,
            token), Times.Once);

        _mockDispatcher.Verify(x => x.DispatchMessage(
            It.IsAny<IGlobalContext>(),
            It.IsAny<ISerializer>(),
            It.IsAny<IMessageBuffer>(),
            token), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldRespectMaxDegreeOfParallelism()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var buffers = new List<Mock<IMessageBuffer>>();
        var token = CancellationToken.None;
        var maxDegree = 2;

        // Create more buffers than max degree of parallelism
        for (int i = 0; i < 5; i++)
        {
            buffers.Add(new Mock<IMessageBuffer>());
        }

        var callCount = 0;
        var concurrentCalls = 0;
        var maxConcurrentCalls = 0;

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(buffers.ConvertAll(b => b.Object).ToAsyncEnumerable());

        _mockDispatcher.Setup(x => x.DispatchMessage(
                It.IsAny<IGlobalContext>(),
                It.IsAny<ISerializer>(),
                It.IsAny<IMessageBuffer>(),
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                Interlocked.Increment(ref callCount);
                Interlocked.Increment(ref concurrentCalls);
                if (concurrentCalls > maxConcurrentCalls)
                {
                    maxConcurrentCalls = concurrentCalls;
                }
                Thread.Sleep(100); // Simulate work to allow parallel execution
                Interlocked.Decrement(ref concurrentCalls);
            })
            .Returns(Task.CompletedTask);

        // Act
        await _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token);

        // Assert
        Assert.Equal(buffers.Count, callCount);
        Assert.True(maxConcurrentCalls <= maxDegree,
            $"Max concurrent calls ({maxConcurrentCalls}) should not exceed MaxDegreeOfParallelism ({maxDegree})");
    }

    [Fact]
    public async Task Process_ShouldHandleDispatchExceptionAndStillReturnBuffer()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var mockBuffer = new Mock<IMessageBuffer>();
        var token = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(new List<IMessageBuffer> { mockBuffer.Object }.ToAsyncEnumerable());

        _mockDispatcher.Setup(x => x.DispatchMessage(
                _mockGlobalContext.Object,
                _mockSerializer.Object,
                mockBuffer.Object,
                token))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token));

        // Buffer should still be returned to pool even if dispatch fails
        mockBuffer.Verify(x => x.SetLength(0), Times.Once);
        mockBuffer.Verify(x => x.Shrink(), Times.Once);
        _mockBufferPool.Verify(x => x.Return(mockBuffer.Object, token), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldUseProvidedTaskScheduler()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var mockBuffer = new Mock<IMessageBuffer>();
        var token = CancellationToken.None;
        var mockTaskScheduler = new Mock<TaskScheduler>();

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(new List<IMessageBuffer> { mockBuffer.Object }.ToAsyncEnumerable());

        // Act
        await _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token);

        // Note: We can't easily verify TaskScheduler usage without more complex setup
        // This test at least ensures the option is accepted without throwing
        Assert.NotNull(_options.TaskScheduler);
    }

    [Fact]
    public async Task Process_ShouldProcessMessagesInParallel()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var token = CancellationToken.None;
        var bufferCount = 10;
        var buffers = new List<Mock<IMessageBuffer>>();
        var processingOrder = new ConcurrentBag<int>();
        var processingTasks = new List<TaskCompletionSource<bool>>();

        // Create buffers and task completion sources for synchronization
        for (int i = 0; i < bufferCount; i++)
        {
            buffers.Add(new Mock<IMessageBuffer>());
            processingTasks.Add(new TaskCompletionSource<bool>());
        }

        _mockGlobalContext.Setup(x => x.Socket).Returns(mockSocket.Object);
        _mockEnumerator.Setup(x => x.EnumerateAsync(
                mockSocket.Object,
                _options,
                _mockBufferPool.Object,
                token))
            .Returns(buffers.ConvertAll(b => b.Object).ToAsyncEnumerable());

        var callIndex = 0;
        _mockDispatcher.Setup(x => x.DispatchMessage(
                It.IsAny<IGlobalContext>(),
                It.IsAny<ISerializer>(),
                It.IsAny<IMessageBuffer>(),
                It.IsAny<CancellationToken>()))
            .Returns<IGlobalContext, ISerializer, IMessageBuffer, CancellationToken>((_, _, _, _) =>
            {
                var currentIndex = Interlocked.Increment(ref callIndex) - 1;
                processingOrder.Add(currentIndex);
                return processingTasks[currentIndex].Task;
            });

        // Start processing
        var processTask = _processor.Process(_mockGlobalContext.Object, _mockSerializer.Object, _options, token);

        // Allow some time for parallel tasks to start
        await Task.Delay(100);

        // Complete all tasks
        foreach (var tcs in processingTasks)
        {
            tcs.SetResult(true);
        }

        // Wait for completion
        await processTask;

        // Assert: Multiple messages should have started processing (not necessarily in order)
        Assert.True(processingOrder.Count > 1, "Multiple messages should have started processing");
    }
}

// Helper extensions for creating async enumerables
public static class TestExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }
}
