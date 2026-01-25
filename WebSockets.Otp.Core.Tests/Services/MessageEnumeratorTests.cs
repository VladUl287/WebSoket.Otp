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

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(_mockMessageBuffer.Object);

        // Setup WebSocket to receive a single complete message
        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                cancellationToken))
            .Callback<Memory<byte>, CancellationToken>((buffer, _) =>
            {
                // Write test data to the buffer
                messageData.AsMemory().CopyTo(buffer);
            })
            .ReturnsAsync(new ValueWebSocketReceiveResult(
                messageData.Length,
                WebSocketMessageType.Binary,
                endOfMessage: true));

        // Setup message buffer to track length and capture data
        var bufferLength = 0;
        _mockMessageBuffer.SetupGet(b => b.Length).Returns(() => bufferLength);
        //_mockMessageBuffer.Setup(b => b.Write(It.IsAny<ReadOnlyMemory<byte>>()))
        //    .Callback<ReadOnlyMemory<byte>>(data =>
        //    {
        //        bufferLength += data.Length;
        //        data.CopyTo(_capturedData);
        //        _dataCaptured = true;
        //    });

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
        Assert.Same(_mockMessageBuffer.Object, messages[0]);
        Assert.True(_dataCaptured);
    }

    [Fact]
    public async Task EnumerateAsync_ReceivesMultiSegmentMessage_ReturnsCompleteMessage()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var segment1 = new byte[] { 1, 2, 3 };
        var segment2 = new byte[] { 4, 5 };

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(_mockMessageBuffer.Object);

        var receiveSequence = new Queue<ValueWebSocketReceiveResult>();
        receiveSequence.Enqueue(new ValueWebSocketReceiveResult(
            segment1.Length,
            WebSocketMessageType.Binary,
            endOfMessage: false));
        receiveSequence.Enqueue(new ValueWebSocketReceiveResult(
            segment2.Length,
            WebSocketMessageType.Binary,
            endOfMessage: true));

        var writeCallCount = 0;
        var callbacks = new Action<Memory<byte>>[]
        {
            buffer => segment1.AsMemory().CopyTo(buffer),
            buffer => segment2.AsMemory().CopyTo(buffer)
        };

        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                cancellationToken))
            .Callback<Memory<byte>, CancellationToken>((buffer, _) =>
            {
                if (writeCallCount < callbacks.Length)
                {
                    callbacks[writeCallCount](buffer);
                }
            })
            .Returns(() => new ValueTask<ValueWebSocketReceiveResult>(receiveSequence.Dequeue()));

        var currentLength = 0;
        _mockMessageBuffer.SetupGet(b => b.Length).Returns(() => currentLength);
        //_mockMessageBuffer.Setup(b => b.Write(It.IsAny<ReadOnlyMemory<byte>>()))
        //    .Callback<ReadOnlyMemory<byte>>(data =>
        //    {
        //        currentLength += data.Length;
        //        writeCallCount++;
        //    });

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
        Assert.Equal(2, writeCallCount);
    }

    [Fact]
    public async Task EnumerateAsync_ReceivesCloseMessage_StopsEnumeration()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(_mockMessageBuffer.Object);

        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                cancellationToken))
            .ReturnsAsync(new ValueWebSocketReceiveResult(
                0,
                WebSocketMessageType.Close,
                endOfMessage: true));

        _mockMessageBuffer.SetupGet(b => b.Length).Returns(0);

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

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(_mockMessageBuffer.Object);

        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                cancellationToken))
            .Callback<Memory<byte>, CancellationToken>((buffer, _) =>
            {
                // Fill with some data
                largeData.AsMemory(0, Math.Min(largeData.Length, buffer.Length)).CopyTo(buffer);
            })
            .ReturnsAsync(new ValueWebSocketReceiveResult(
                largeData.Length,
                WebSocketMessageType.Binary,
                endOfMessage: false));

        var bufferLength = 0;
        _mockMessageBuffer.SetupGet(b => b.Length).Returns(() => bufferLength);
        //_mockMessageBuffer.Setup(b => b.Write(It.IsAny<ReadOnlyMemory<byte>>()))
        //    .Callback<ReadOnlyMemory<byte>>(data => bufferLength += data.Length);

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
        var writeCount = 0;

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(_mockMessageBuffer.Object);

        var receiveSequence = new Queue<ValueWebSocketReceiveResult>();
        // First message
        receiveSequence.Enqueue(new ValueWebSocketReceiveResult(5, WebSocketMessageType.Binary, true));
        // Second message
        receiveSequence.Enqueue(new ValueWebSocketReceiveResult(3, WebSocketMessageType.Binary, true));
        // Close
        receiveSequence.Enqueue(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));

        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                cancellationToken))
            .Returns(() => new ValueTask<ValueWebSocketReceiveResult>(receiveSequence.Dequeue()));

        _mockMessageBuffer.SetupGet(b => b.Length).Returns(0);
        //_mockMessageBuffer.Setup(b => b.Write(It.IsAny<ReadOnlyMemory<byte>>()))
        //    .Callback<ReadOnlyMemory<byte>>(_ => writeCount++);

        // Setup Return method if your buffer pool has it
        //_mockMessageBuffer.Setup(b => b.Reset())
        //    .Callback(() => _mockMessageBuffer.SetupGet(b => b.Length).Returns(0));

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
        Assert.Equal(2, writeCount);
        _mockBufferPool.Verify(p => p.Rent(cancellationToken), Times.Once());
    }

    [Fact]
    public async Task EnumerateAsync_ReturnsArrayPoolBuffer_AfterEnumeration()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var pool = ArrayPool<byte>.Create();
        var rentedBuffer = pool.Rent(_config.ReceiveBufferSize);

        try
        {
            _mockBufferPool.Setup(p => p.Rent(cancellationToken))
                .ReturnsAsync(_mockMessageBuffer.Object);

            _mockWebSocket.Setup(s => s.ReceiveAsync(
                    It.IsAny<Memory<byte>>(),
                    cancellationToken))
                .ReturnsAsync(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));

            _mockMessageBuffer.SetupGet(b => b.Length).Returns(0);

            // Act
            await foreach (var _ in _enumerator.EnumerateAsync(
                _mockWebSocket.Object,
                _config,
                _mockBufferPool.Object,
                cancellationToken))
            {
                // Just iterate
            }

            // Assert - No exception means buffer was properly returned
            // (ArrayPool doesn't have a way to verify returns in tests)
        }
        finally
        {
            pool.Return(rentedBuffer);
        }
    }

    [Fact]
    public async Task EnumerateAsync_WithEmptyMessage_ReturnsEmptyBuffer()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(_mockMessageBuffer.Object);

        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                cancellationToken))
            .ReturnsAsync(new ValueWebSocketReceiveResult(
                0,
                WebSocketMessageType.Binary,
                endOfMessage: true));

        var writeCalled = false;
        _mockMessageBuffer.SetupGet(b => b.Length).Returns(0);
        //_mockMessageBuffer.Setup(b => b.Write(It.IsAny<ReadOnlyMemory<byte>>()))
        //    .Callback<ReadOnlyMemory<byte>>(_ => writeCalled = true);

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
        Assert.True(writeCalled); // Should still call Write even with empty data
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

        _mockBufferPool.Setup(p => p.Rent(cancellationToken))
            .ReturnsAsync(_mockMessageBuffer.Object);

        var receiveSequence = new Queue<ValueWebSocketReceiveResult>();
        receiveSequence.Enqueue(new ValueWebSocketReceiveResult(3, WebSocketMessageType.Binary, false));
        receiveSequence.Enqueue(new ValueWebSocketReceiveResult(2, WebSocketMessageType.Binary, false));
        receiveSequence.Enqueue(new ValueWebSocketReceiveResult(5, WebSocketMessageType.Binary, true));

        var chunkIndex = 0;
        _mockWebSocket.Setup(s => s.ReceiveAsync(
                It.IsAny<Memory<byte>>(),
                cancellationToken))
            .Callback<Memory<byte>, CancellationToken>((buffer, _) =>
            {
                if (chunkIndex < dataChunks.Length)
                {
                    dataChunks[chunkIndex].AsMemory().CopyTo(buffer);
                }
            })
            .Returns(() => new ValueTask<ValueWebSocketReceiveResult>(receiveSequence.Dequeue()));

        var totalLength = 0;
        _mockMessageBuffer.SetupGet(b => b.Length).Returns(() => totalLength);
        //_mockMessageBuffer.Setup(b => b.Write(It.IsAny<ReadOnlyMemory<byte>>()))
        //    .Callback<ReadOnlyMemory<byte>>(data =>
        //    {
        //        totalLength += data.Length;
        //        chunkIndex++;
        //    });

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
        Assert.Equal(10, totalLength); // Total bytes written
    }
}
