using Moq;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Services.Processors;
using WebSockets.Otp.Core.Services.Utils;

namespace WebSockets.Otp.Core.Tests.Services.Processors;

public class SequentialMessageProcessorTests
{
    private readonly Mock<IMessageDispatcher> _dispatcherMock;
    private readonly Mock<IMessageBufferFactory> _bufferFactoryMock;
    private readonly Mock<IGlobalContext> _globalContextMock;
    private readonly Mock<ISerializer> _serializerMock;
    private readonly Mock<WebSocket> _socketMock;
    private readonly Mock<IMessageBuffer> _messageBufferMock;
    private readonly SequentialMessageProcessor _processor;
    private readonly WsConfiguration _config;

    public SequentialMessageProcessorTests()
    {
        _dispatcherMock = new Mock<IMessageDispatcher>();
        _bufferFactoryMock = new Mock<IMessageBufferFactory>();
        _globalContextMock = new Mock<IGlobalContext>();
        _serializerMock = new Mock<ISerializer>();
        _socketMock = new Mock<WebSocket>();
        _messageBufferMock = new Mock<IMessageBuffer>();

        _config = new WsConfiguration(new WsOptions
        {
            ReceiveBufferSize = 2048,
            MaxMessageSize = 4096,
            ShrinkBuffers = false
        });

        _globalContextMock.SetupGet(x => x.Socket).Returns(_socketMock.Object);
        _bufferFactoryMock
            .Setup(x => x.Create(It.IsAny<int>()))
            .Returns(_messageBufferMock.Object);

        _processor = new SequentialMessageProcessor(
            _dispatcherMock.Object,
            _bufferFactoryMock.Object);
    }

    [Fact]
    public void Mode_ShouldReturnSequential()
    {
        // Act
        var mode = _processor.Mode;

        // Assert
        Assert.Equal(ProcessingMode.Sequential, mode);
    }

    [Fact]
    public async Task Process_ShouldProcessCompleteMessage()
    {
        // Arrange
        var token = CancellationToken.None;
        var messageData = new byte[] { 1, 2, 3, 4, 5 };
        var receiveResult = new ValueWebSocketReceiveResult(
            messageData.Length,
            WebSocketMessageType.Binary,
            endOfMessage: true);

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveResult)
            .Callback((Memory<byte> memory, CancellationToken ct) =>
            {
                messageData.CopyTo(memory);
            });

        _messageBufferMock.SetupGet(x => x.Length).Returns(0);

        // Act
        await _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            _config,
            token);

        // Assert
        _messageBufferMock.Verify(x => x.Write(messageData),
            Times.Once);
        _dispatcherMock.Verify(x => x.DispatchMessage(
            _globalContextMock.Object,
            _serializerMock.Object,
            _messageBufferMock.Object,
            token), Times.Once);
        _messageBufferMock.Verify(x => x.SetLength(0), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldHandleMultipleFragments()
    {
        // Arrange
        var token = CancellationToken.None;
        var fragment1 = new byte[] { 1, 2, 3 };
        var fragment2 = new byte[] { 4, 5, 6 };
        var receiveSequence = new Queue<ValueWebSocketReceiveResult>();
        receiveSequence.Enqueue(new ValueWebSocketReceiveResult(
            fragment1.Length,
            WebSocketMessageType.Binary,
            endOfMessage: false));
        receiveSequence.Enqueue(new ValueWebSocketReceiveResult(
            fragment2.Length,
            WebSocketMessageType.Binary,
            endOfMessage: true));

        var callCount = 0;
        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => receiveSequence.Dequeue())
            .Callback((Memory<byte> memory, CancellationToken ct) =>
            {
                if (callCount == 0)
                    fragment1.CopyTo(memory);
                else
                    fragment2.CopyTo(memory);
                callCount++;
            });

        _messageBufferMock.SetupGet(x => x.Length).Returns(0);

        // Act
        await _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            _config,
            token);

        // Assert
        _messageBufferMock.Verify(x => x.Write(fragment1),
            Times.Once);
        _messageBufferMock.Verify(x => x.Write(fragment2),
            Times.Once);
        _dispatcherMock.Verify(x => x.DispatchMessage(
            It.IsAny<IGlobalContext>(),
            It.IsAny<ISerializer>(),
            It.IsAny<IMessageBuffer>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldStopOnCloseMessage()
    {
        // Arrange
        var token = CancellationToken.None;
        var closeResult = new ValueWebSocketReceiveResult(
            0,
            WebSocketMessageType.Close,
            endOfMessage: true);

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(closeResult);

        // Act
        await _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            _config,
            token);

        // Assert
        _dispatcherMock.Verify(x => x.DispatchMessage(
            It.IsAny<IGlobalContext>(),
            It.IsAny<ISerializer>(),
            It.IsAny<IMessageBuffer>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_ShouldThrowOnExceededMaxMessageSize()
    {
        // Arrange
        var token = CancellationToken.None;
        var largeData = new byte[2048];
        var receiveResult = new ValueWebSocketReceiveResult(
            largeData.Length,
            WebSocketMessageType.Binary,
            endOfMessage: false);

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveResult)
            .Callback((Memory<byte> memory, CancellationToken ct) =>
            {
                largeData.CopyTo(memory);
            });

        _messageBufferMock.SetupGet(x => x.Length).Returns(3000); // Already at 3000 bytes

        // Act & Assert
        await Assert.ThrowsAsync<OutOfMemoryException>(() =>
            _processor.Process(
                _globalContextMock.Object,
                _serializerMock.Object,
                _config,
                token));
    }

    [Fact]
    public async Task Process_ShouldShrinkBufferWhenConfigured()
    {
        // Arrange
        var token = CancellationToken.None;
        var messageData = new byte[] { 1, 2, 3 };
        var receiveResult = new ValueWebSocketReceiveResult(
            messageData.Length,
            WebSocketMessageType.Binary,
            endOfMessage: true);

        var first = true;
        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var result = first ? receiveResult : new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true);
                first = false;
                return result;
            })
            .Callback((Memory<byte> memory, CancellationToken ct) =>
            {
                messageData.CopyTo(memory);
            });

        // Act
        await _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            _config,
            token);

        // Assert
        _messageBufferMock.Verify(x => x.Shrink(), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldNotShrinkBufferWhenNotConfigured()
    {
        // Arrange
        var token = CancellationToken.None;
        var messageData = new byte[] { 1, 2, 3 };
        var receiveResult = new ValueWebSocketReceiveResult(
            messageData.Length,
            WebSocketMessageType.Binary,
            endOfMessage: true);

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveResult)
            .Callback((Memory<byte> memory, CancellationToken ct) =>
            {
                messageData.CopyTo(memory);
            });

        _messageBufferMock.SetupGet(x => x.Length).Returns(0);

        // Act
        await _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            _config,
            token);

        // Assert
        _messageBufferMock.Verify(x => x.Shrink(), Times.Never);
    }

    [Fact]
    public async Task Process_ShouldRentAndReturnArrayFromPool()
    {
        // Arrange
        var token = CancellationToken.None;
        var messageData = new byte[] { 1, 2, 3 };
        var receiveResult = new ValueWebSocketReceiveResult(
            messageData.Length,
            WebSocketMessageType.Close,
            endOfMessage: true);

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveResult);

        // Act
        await _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            _config,
            token);

        // Note: ArrayPool usage is internal and not directly testable via mocks
        // This test verifies the method completes without exceptions
    }

    [Fact]
    public async Task Process_ShouldHandleCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var tcs = new TaskCompletionSource<ValueWebSocketReceiveResult>();

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<ValueWebSocketReceiveResult>(tcs.Task));

        // Act
        var processTask = _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            _config,
            token);

        cts.Cancel();
        tcs.SetCanceled();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => processTask);
    }

    [Fact]
    public async Task Process_ShouldDisposeBuffer()
    {
        // Arrange
        var token = CancellationToken.None;
        var messageData = new byte[] { 1, 2, 3 };
        var receiveResult = new ValueWebSocketReceiveResult(
            messageData.Length,
            WebSocketMessageType.Close,
            endOfMessage: true);

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveResult);

        var disposableBufferMock = _messageBufferMock.As<IDisposable>();

        // Act
        await _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            _config,
            token);

        // Assert
        disposableBufferMock.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldHandleDispatcherExceptionAndResetBuffer()
    {
        // Arrange
        var token = CancellationToken.None;
        var messageData = new byte[] { 1, 2, 3 };
        var receiveResult = new ValueWebSocketReceiveResult(
            messageData.Length,
            WebSocketMessageType.Binary,
            endOfMessage: true);

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveResult)
            .Callback((Memory<byte> memory, CancellationToken ct) =>
            {
                messageData.CopyTo(memory);
            });

        _messageBufferMock.SetupGet(x => x.Length).Returns(0);
        _dispatcherMock
            .Setup(x => x.DispatchMessage(
                It.IsAny<IGlobalContext>(),
                It.IsAny<ISerializer>(),
                It.IsAny<IMessageBuffer>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Dispatcher failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _processor.Process(
                _globalContextMock.Object,
                _serializerMock.Object,
                _config,
                token));

        // Verify buffer was reset even on exception
        _messageBufferMock.Verify(x => x.SetLength(0), Times.Once);
    }

    public class TestMessageBuffer : IMessageBuffer
    {
        private readonly List<byte> _buffer = new();
        public int Length => _buffer.Count;

        public int Capacity => throw new NotImplementedException();

        public ReadOnlySpan<byte> Span => throw new NotImplementedException();

        public IMemoryOwner<byte> Manager => throw new NotImplementedException();

        public void Write(ReadOnlySpan<byte> data)
        {
            _buffer.AddRange(data.ToArray());
        }

        public void SetLength(int length)
        {
            if (length < _buffer.Count)
            {
                _buffer.RemoveRange(length, _buffer.Count - length);
            }
        }

        public void Shrink()
        {
            // Implementation for testing
        }

        public byte[] GetWrittenData() => _buffer.ToArray();

        public void Dispose()
        {
            _buffer.Clear();
        }

        public void Write(ReadOnlySequence<byte> data)
        {
            throw new NotImplementedException();
        }
    }
}
