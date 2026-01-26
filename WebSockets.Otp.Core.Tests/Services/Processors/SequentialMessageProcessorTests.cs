using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Services.Processors;

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
            _bufferFactoryMock.Object,
            NullLogger<SequentialMessageProcessor>.Instance);
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
        var receiveResult = new WebSocketReceiveResult(
            messageData.Length,
            WebSocketMessageType.Binary,
            endOfMessage: true);

        var bufferMock = new TestMessageBuffer();
        _bufferFactoryMock
            .Setup(c => c.Create(It.IsAny<int>()))
            .Returns(bufferMock);

        var first = true;
        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var result = first ? receiveResult : new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
                first = false;
                return result;
            })
            .Callback((ArraySegment<byte> memory, CancellationToken ct) =>
            {
                messageData.CopyTo(memory.AsSpan());
            });

        // Act
        await _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            _config,
            token);

        // Assert
        Assert.Equal(1, bufferMock.GetCount(messageData));
        Assert.Equal(1, bufferMock.SetLengthCount);
        _dispatcherMock.Verify(x => x.DispatchMessage(
            _globalContextMock.Object,
            _serializerMock.Object,
            bufferMock,
            token), Times.Once);
    }

    [Fact]
    public async Task Process_ShouldHandleMultipleFragments()
    {
        // Arrange
        var token = CancellationToken.None;
        var fragment1 = new byte[] { 1, 2, 3 };
        var fragment2 = new byte[] { 4, 5, 6 };
        var receiveSequence = new Queue<WebSocketReceiveResult>();
        receiveSequence.Enqueue(new WebSocketReceiveResult(
            fragment1.Length,
            WebSocketMessageType.Binary,
            endOfMessage: false));
        receiveSequence.Enqueue(new WebSocketReceiveResult(
            fragment2.Length,
            WebSocketMessageType.Binary,
            endOfMessage: true));

        var bufferMock = new TestMessageBuffer();
        _bufferFactoryMock
            .Setup(c => c.Create(It.IsAny<int>()))
            .Returns(bufferMock);

        var callCount = 0;
        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (receiveSequence.TryDequeue(out var result))
                {
                    return result;
                }
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            })
            .Callback((ArraySegment<byte> memory, CancellationToken ct) =>
            {
                if (callCount == 0)
                    fragment1.CopyTo(memory.AsSpan());
                else
                    fragment2.CopyTo(memory.AsSpan());
                callCount++;
            });

        // Act
        await _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            _config,
            token);

        // Assert
        Assert.Equal(1, bufferMock.GetCount(fragment1));
        Assert.Equal(1, bufferMock.GetCount(fragment2));
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
        var closeResult = new WebSocketReceiveResult(
            0,
            WebSocketMessageType.Close,
            endOfMessage: true);

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
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
        var receiveResult = new WebSocketReceiveResult(
            largeData.Length,
            WebSocketMessageType.Binary,
            endOfMessage: false);

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveResult)
            .Callback((ArraySegment<byte> memory, CancellationToken ct) =>
            {
                largeData.CopyTo(memory.AsSpan());
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
        var receiveResult = new WebSocketReceiveResult(
            messageData.Length,
            WebSocketMessageType.Binary,
            endOfMessage: true);

        using var bufferMock = new TestMessageBuffer();
        _bufferFactoryMock
            .Setup(c => c.Create(It.IsAny<int>()))
            .Returns(bufferMock);

        var first = true;
        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var result = first ? receiveResult : new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
                first = false;
                return result;
            })
            .Callback((ArraySegment<byte> memory, CancellationToken ct) =>
            {
                messageData.CopyTo(memory.AsSpan());
            });

        // Act
        await _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            new WsConfiguration(new WsOptions
            {
                ShrinkBuffers = true
            }),
            token);

        // Assert
        Assert.Equal(1, bufferMock.ShrinkCount);
    }

    [Fact]
    public async Task Process_ShouldNotShrinkBufferWhenNotConfigured()
    {
        // Arrange
        var token = CancellationToken.None;
        var messageData = new byte[] { 1, 2, 3 };
        var receiveResult = new WebSocketReceiveResult(
            messageData.Length,
            WebSocketMessageType.Binary,
            endOfMessage: true);

        using var bufferMock = new TestMessageBuffer();
        _bufferFactoryMock
            .Setup(c => c.Create(It.IsAny<int>()))
            .Returns(bufferMock);

        var first = true;
        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var result = first ? receiveResult : new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
                first = false;
                return result;
            })
            .Callback((ArraySegment<byte> memory, CancellationToken ct) =>
            {
                messageData.CopyTo(memory.AsSpan());
            });

        // Act
        await _processor.Process(
            _globalContextMock.Object,
            _serializerMock.Object,
            _config,
            token);

        // Assert
        Assert.Equal(0, bufferMock.ShrinkCount);
    }

    [Fact]
    public async Task Process_ShouldHandleCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var tcs = new TaskCompletionSource<WebSocketReceiveResult>();

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

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
        var receiveResult = new WebSocketReceiveResult(
            messageData.Length,
            WebSocketMessageType.Close,
            endOfMessage: true);

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
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
        var receiveResult = new WebSocketReceiveResult(
            messageData.Length,
            WebSocketMessageType.Binary,
            endOfMessage: true);

        using var bufferMock = new TestMessageBuffer();
        _bufferFactoryMock
            .Setup(c => c.Create(It.IsAny<int>()))
            .Returns(bufferMock);

        _socketMock
            .Setup(x => x.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveResult)
            .Callback((ArraySegment<byte> memory, CancellationToken ct) =>
            {
                messageData.CopyTo(memory.AsSpan());
            });

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
        Assert.Equal(1, bufferMock.SetLengthCount);
    }

    public class TestMessageBuffer : MemoryManager<byte>, IMessageBuffer
    {
        public int ShrinkCount = 0;
        public int SetLengthCount = 0;

        public Dictionary<int, int> CallMap = new();

        public int Length => 0;

        public int Capacity => throw new NotImplementedException();

        public Span<byte> Span => throw new NotImplementedException();

        public override Span<byte> GetSpan()
        {
            throw new NotImplementedException();
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            throw new NotImplementedException();
        }

        public void SetLength(int length)
        {
            SetLengthCount++;
        }

        public void Shrink()
        {
            ShrinkCount++;
        }

        public override void Unpin()
        { }

        public int GetCount(ReadOnlySpan<byte> data)
        {
            var key = string.Join("", data.ToArray()).GetHashCode();
            return CallMap[key];
        }

        public void Write(ReadOnlySpan<byte> data)
        {
            var key = string.Join("", data.ToArray()).GetHashCode();
            if (!CallMap.TryAdd(key, 1))
                CallMap[key] += 1;
        }

        public void Write(ReadOnlySequence<byte> data)
        {
            var key = string.Join("", data.ToArray()).GetHashCode();
            if (!CallMap.TryAdd(key, 1))
                CallMap[key] += 1;
        }

        protected override void Dispose(bool disposing)
        { }
    }
}
