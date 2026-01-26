using Moq;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Services;

namespace WebSockets.Otp.Core.Tests.Services;

public class MessageEnumeratorTests
{
    private readonly Mock<WebSocket> _mockWebSocket;
    private readonly Mock<IAsyncObjectPool<IMessageBuffer>> _mockBufferPool;
    private readonly Mock<IMessageBuffer> _mockMessageBuffer;
    private readonly WsConfiguration _config;
    private readonly MessageEnumerator _enumerator;
    private readonly Memory<byte> _capturedData;
    private bool _dataCaptured;

    public MessageEnumeratorTests()
    {
        _mockWebSocket = new Mock<WebSocket>();
        _mockBufferPool = new Mock<IAsyncObjectPool<IMessageBuffer>>();
        _mockMessageBuffer = new Mock<IMessageBuffer>();
        _config = new WsConfiguration(new WsOptions
        {
            ReceiveBufferSize = 1024,
            MaxMessageSize = 8192
        });
        _enumerator = new MessageEnumerator();
        _capturedData = new Memory<byte>(new byte[4096]);
        _dataCaptured = false;
    }

    [Fact]
    public async Task EnumerateAsync_ReceivesCompleteMessage_ReturnsMessageBuffer()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var messageData = new byte[] { 1, 2, 3, 4, 5 };

        var messageBuffer = new TestMessageBuffer();

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(messageBuffer);

        // Setup WebSocket to receive a single complete message
        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                cancellationToken))
            .Callback<ArraySegment<byte>, CancellationToken>((buffer, _) =>
            {
                // Write test data to the buffer
                messageData.AsMemory().CopyTo(buffer);
            })
            .ReturnsAsync(new WebSocketReceiveResult(
                messageData.Length,
                WebSocketMessageType.Binary,
                endOfMessage: true));

        // Act
        var messages = new List<IMessageBuffer>();
        await foreach (var message in _enumerator.EnumerateAsync(
            _mockWebSocket.Object,
            _config,
            _mockBufferPool.Object,
            cancellationToken))
        {
            messages.Add(message);
            break; // Only process first message for test
        }

        // Assert
        Assert.Single(messages);
    }

    [Fact]
    public async Task EnumerateAsync_ReceivesCloseMessage_StopsEnumeration()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        var messageBuffer = new TestMessageBuffer();

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(messageBuffer);

        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                cancellationToken))
            .ReturnsAsync(new WebSocketReceiveResult(
                0,
                WebSocketMessageType.Close,
                endOfMessage: true));

        // Act
        var messages = new List<IMessageBuffer>();
        await foreach (var message in _enumerator.EnumerateAsync(
            _mockWebSocket.Object,
            _config,
            _mockBufferPool.Object,
            cancellationToken))
        {
            messages.Add(message);
        }

        // Assert
        Assert.Empty(messages); // Should not yield any messages on close
    }

    [Fact]
    public async Task EnumerateAsync_MessageExceedsMaxSize_ThrowsOutOfMemoryException()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var largeData = new byte[_config.MaxMessageSize + 1];

        var messageBuffer = new TestMessageBuffer();

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(messageBuffer);

        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                cancellationToken))
            .Callback<ArraySegment<byte>, CancellationToken>((buffer, _) =>
            {
                messageBuffer.Length += _config.ReceiveBufferSize;
                largeData.AsSpan(0, _config.ReceiveBufferSize).CopyTo(buffer.AsSpan());
            })
            .ReturnsAsync(new WebSocketReceiveResult(
                largeData.Length,
                WebSocketMessageType.Binary,
                endOfMessage: false));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OutOfMemoryException>(async () =>
        {
            await foreach (var _ in _enumerator.EnumerateAsync(
                _mockWebSocket.Object,
                _config,
                _mockBufferPool.Object,
                cancellationToken))
            {
                // Just iterate
            }
        });

        Assert.Contains(_config.MaxMessageSize.ToString(), exception.Message);
    }

    [Fact]
    public async Task EnumerateAsync_CancellationRequested_StopsEnumeration()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(_mockMessageBuffer.Object);

        // Use a TaskCompletionSource to control the async flow
        var tcs = new TaskCompletionSource<ValueWebSocketReceiveResult>();

        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                cancellationToken))
            .Returns(new ValueTask<ValueWebSocketReceiveResult>(tcs.Task));

        _mockMessageBuffer.SetupGet(b => b.Length).Returns(0);

        // Act - start enumeration then cancel
        var enumerationTask = Task.Run(async () =>
        {
            var messages = new List<IMessageBuffer>();
            await foreach (var message in _enumerator.EnumerateAsync(
                _mockWebSocket.Object,
                _config,
                _mockBufferPool.Object,
                cancellationToken))
            {
                messages.Add(message);
            }
            return messages;
        });

        // Cancel immediately
        cts.Cancel();
        // Complete the task to unblock the ReceiveAsync call
        tcs.SetCanceled(cancellationToken);

        // Assert - Should complete without throwing (graceful cancellation)
        var messages = await enumerationTask;
        Assert.Empty(messages);
    }

    [Fact]
    public async Task EnumerateAsync_ReusesMessageBuffer_ForMultipleMessages()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var messageCount = 0;

        var messageBuffer = new TestMessageBuffer();

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(messageBuffer);

        var receiveSequence = new Queue<WebSocketReceiveResult>();
        // First message
        receiveSequence.Enqueue(new WebSocketReceiveResult(5, WebSocketMessageType.Binary, true));
        // Second message
        receiveSequence.Enqueue(new WebSocketReceiveResult(3, WebSocketMessageType.Binary, true));
        // Close
        receiveSequence.Enqueue(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));

        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(receiveSequence.Dequeue()));

        // Act
        var messages = new List<IMessageBuffer>();
        await foreach (var message in _enumerator.EnumerateAsync(
            _mockWebSocket.Object,
            _config,
            _mockBufferPool.Object,
            cancellationToken))
        {
            messages.Add(message);
            messageCount++;
            if (messageCount >= 2) break;
        }

        // Assert - Should rent buffer once but write multiple times
        Assert.Equal(2, messages.Count);
        Assert.Equal(2, messageBuffer.WrtieMap.Single().Value);
        _mockBufferPool.Verify(p => p.Rent(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EnumerateAsync_WithEmptyMessage_ReturnsEmptyBuffer()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        var messageBuffer = new TestMessageBuffer();

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(messageBuffer);

        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                cancellationToken))
            .ReturnsAsync(new WebSocketReceiveResult(
                0,
                WebSocketMessageType.Binary,
                endOfMessage: true));

        // Act
        var messages = new List<IMessageBuffer>();
        await foreach (var message in _enumerator.EnumerateAsync(
            _mockWebSocket.Object,
            _config,
            _mockBufferPool.Object,
            cancellationToken))
        {
            messages.Add(message);
            break;
        }

        // Assert
        Assert.Single(messages);
    }

    [Fact]
    public async Task EnumerateAsync_HandlesPartialReceives_AccumulatesData()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var dataChunks = new[]
        {
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5 },
            new byte[] { 6, 7, 8, 9, 10 }
        };

        var messageBuffer = new TestMessageBuffer();

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(messageBuffer);

        var receiveSequence = new Queue<WebSocketReceiveResult>();
        receiveSequence.Enqueue(new WebSocketReceiveResult(3, WebSocketMessageType.Binary, false));
        receiveSequence.Enqueue(new WebSocketReceiveResult(2, WebSocketMessageType.Binary, false));
        receiveSequence.Enqueue(new WebSocketReceiveResult(5, WebSocketMessageType.Binary, true));

        var chunkIndex = 0;
        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<ArraySegment<byte>>(),
                cancellationToken))
            .Callback<ArraySegment<byte>, CancellationToken>((buffer, _) =>
            {
                if (chunkIndex < dataChunks.Length)
                {
                    dataChunks[chunkIndex].AsMemory().CopyTo(buffer);
                    messageBuffer.Length += dataChunks[chunkIndex].Length;
                    chunkIndex++;
                }
            })
            .Returns(() => Task.FromResult(receiveSequence.Dequeue()));

        // Act
        var messages = new List<IMessageBuffer>();
        await foreach (var message in _enumerator.EnumerateAsync(
            _mockWebSocket.Object,
            _config,
            _mockBufferPool.Object,
            cancellationToken))
        {
            messages.Add(message);
            break;
        }

        // Assert
        Assert.Single(messages);
        Assert.Equal(3, chunkIndex); // Should have written 3 chunks
        Assert.Equal(10, messageBuffer.Length); // Total bytes written
    }

    public class TestMessageBuffer : MemoryManager<byte>, IMessageBuffer
    {
        public int ShrinkCount = 0;
        public int SetLengthCount = 0;

        public Dictionary<int, int> WrtieMap = new();

        public int Length { get; set; } = 0;

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
            return WrtieMap[key];
        }

        public void Write(ReadOnlySpan<byte> data)
        {
            var key = string.Join("", data.ToArray()).GetHashCode();
            if (!WrtieMap.TryAdd(key, 1))
                WrtieMap[key] += 1;
        }

        public void Write(ReadOnlySequence<byte> data)
        {
            var key = string.Join("", data.ToArray()).GetHashCode();
            if (!WrtieMap.TryAdd(key, 1))
                WrtieMap[key] += 1;
        }

        protected override void Dispose(bool disposing)
        { }
    }
}
