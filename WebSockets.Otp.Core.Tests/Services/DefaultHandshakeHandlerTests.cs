using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Services;

namespace WebSockets.Otp.Core.Tests.Services;

public class DefaultHandshakeHandlerTests
{
    private readonly Mock<ISerializerStore> _storeMock;
    private readonly Mock<IMessageEnumerator> _enumeratorMock;
    private readonly Mock<IAsyncObjectPool<IMessageBuffer>> _objectPoolMock;
    private readonly DefaultHandshakeHandler _handler;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<WebSocket> _webSocketMock;

    public DefaultHandshakeHandlerTests()
    {
        _storeMock = new Mock<ISerializerStore>();
        _enumeratorMock = new Mock<IMessageEnumerator>();
        _objectPoolMock = new Mock<IAsyncObjectPool<IMessageBuffer>>();

        var logger = NullLogger<DefaultHandshakeHandler>.Instance;

        _handler = new DefaultHandshakeHandler(
            _storeMock.Object,
            _enumeratorMock.Object,
            _objectPoolMock.Object,
            logger
        );

        _httpContextMock = new Mock<HttpContext>();
        _webSocketMock = new Mock<WebSocket>();
    }

    [Fact]
    public async Task HandleAsync_ReturnsNull_WhenNoHandshakeMessage()
    {
        // Arrange
        var options = new WsConfiguration(new WsOptions());
        var token = CancellationToken.None;

        // Create empty async enumerable
        var emptyEnumerable = Enumerable.Empty<IMessageBuffer>().ToAsyncEnumerable();

        _enumeratorMock
            .Setup(e => e.EnumerateAsync(_webSocketMock.Object, options, _objectPoolMock.Object, token))
            .Returns(emptyEnumerable);

        // Act
        var result = await _handler.HandleAsync(_httpContextMock.Object, _webSocketMock.Object, options, token);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNull_WhenSerializerNotFound()
    {
        // Arrange
        var options = new WsConfiguration(new WsOptions());
        var token = CancellationToken.None;
        var bufferMock = new Mock<IMessageBuffer>();

        // Setup buffer with some data
        bufferMock.SetupGet(b => b.Length).Returns(10);

        var messageBuffer = bufferMock.Object;
        var enumerable = new[] { messageBuffer }.ToAsyncEnumerable();

        _enumeratorMock
            .Setup(e => e.EnumerateAsync(_webSocketMock.Object, options, _objectPoolMock.Object, token))
            .Returns(enumerable);

        _storeMock
            .Setup(s => s.TryGet("json", out It.Ref<ISerializer?>.IsAny))
            .Returns(false);

        // Act
        var result = await _handler.HandleAsync(_httpContextMock.Object, _webSocketMock.Object, options, token);

        // Assert
        Assert.Null(result);
        _storeMock.Verify(s => s.TryGet("json", out It.Ref<ISerializer?>.IsAny), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNull_WhenDeserializationFails()
    {
        // Arrange
        var options = new WsConfiguration(new WsOptions());
        var token = CancellationToken.None;
        var bufferMock = new Mock<IMessageBuffer>();
        var serializerMock = new Mock<ISerializer>();

        // Setup buffer
        var bufferData = new byte[] { 1, 2, 3 };
        bufferMock.SetupGet(b => b.Length).Returns(bufferData.Length);

        var messageBuffer = bufferMock.Object;
        var enumerable = new[] { messageBuffer }.ToAsyncEnumerable();

        _enumeratorMock
            .Setup(e => e.EnumerateAsync(_webSocketMock.Object, options, _objectPoolMock.Object, token))
            .Returns(enumerable);

        _storeMock
            .Setup(s => s.TryGet("json", out It.Ref<ISerializer?>.IsAny))
            .Callback((string protocol, out ISerializer? serializer) =>
            {
                serializer = serializerMock.Object;
            })
            .Returns(true);

        serializerMock
            .Setup(s => s.Deserialize(typeof(WsHandshakeOptions), bufferData))
            .Returns(null);

        // Act
        var result = await _handler.HandleAsync(_httpContextMock.Object, _webSocketMock.Object, options, token);

        // Assert
        Assert.Null(result);
        serializerMock.Verify(s => s.Deserialize(typeof(WsHandshakeOptions), bufferData), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsHandshakeOptions_WhenSuccessful()
    {
        // Arrange
        var options = new WsConfiguration(new WsOptions());
        var token = CancellationToken.None;
        var bufferMock = new Mock<IMessageBuffer>();
        var serializerMock = new Mock<ISerializer>();
        var expectedHandshakeOptions = new WsHandshakeOptions();

        // Setup buffer
        var bufferData = "{}"u8.ToArray();
        bufferMock.SetupGet(b => b.Length).Returns(bufferData.Length);

        // Setup disposal
        bufferMock.Setup(b => b.Dispose()).Verifiable();

        var messageBuffer = bufferMock.Object;
        var enumerable = new[] { messageBuffer }.ToAsyncEnumerable();

        _enumeratorMock
            .Setup(e => e.EnumerateAsync(_webSocketMock.Object, options, _objectPoolMock.Object, token))
            .Returns(enumerable);

        _storeMock
            .Setup(s => s.TryGet("json", out It.Ref<ISerializer?>.IsAny))
            .Callback((string protocol, out ISerializer? serializer) =>
            {
                serializer = serializerMock.Object;
            })
            .Returns(true);

        serializerMock
            .Setup(s => s.Deserialize(typeof(WsHandshakeOptions), bufferData))
            .Returns(expectedHandshakeOptions);

        var qa = "{}"u8.ToArray();
        _webSocketMock
            .Setup(s => s.SendAsync(
                It.Is<ReadOnlyMemory<byte>>(m => m.ToArray().SequenceEqual(qa)),
                WebSocketMessageType.Text,
                true,
                token
            ))
            .Returns(ValueTask.CompletedTask)
            .Verifiable();

        // Act
        var result = await _handler.HandleAsync(_httpContextMock.Object, _webSocketMock.Object, options, token);

        // Assert
        Assert.Same(expectedHandshakeOptions, result);
        bufferMock.Verify(b => b.Dispose(), Times.Once);
        _webSocketMock.Verify();
    }

    [Fact]
    public async Task HandleAsync_SendsResponse_WhenHandshakeSuccessful()
    {
        // Arrange
        var options = new WsConfiguration(new WsOptions());
        var token = CancellationToken.None;
        var bufferMock = new Mock<IMessageBuffer>();
        var serializerMock = new Mock<ISerializer>();
        var expectedHandshakeOptions = new WsHandshakeOptions();

        // Setup buffer
        var bufferData = "{}"u8.ToArray();
        bufferMock.SetupGet(b => b.Length).Returns(bufferData.Length);
        //bufferMock.SetupGet(b => b.Span).Returns(bufferData.AsSpan());

        var messageBuffer = bufferMock.Object;
        var enumerable = new[] { messageBuffer }.ToAsyncEnumerable();

        _enumeratorMock
            .Setup(e => e.EnumerateAsync(_webSocketMock.Object, options, _objectPoolMock.Object, token))
            .Returns(enumerable);

        _storeMock
            .Setup(s => s.TryGet("json", out It.Ref<ISerializer?>.IsAny))
            .Callback((string protocol, out ISerializer? serializer) =>
            {
                serializer = serializerMock.Object;
            })
            .Returns(true);

        serializerMock
            .Setup(s => s.Deserialize(typeof(WsHandshakeOptions), bufferData))
            .Returns(expectedHandshakeOptions);

        var qa = "{}"u8.ToArray();
        var sendCalled = false;
        _webSocketMock
            .Setup(s => s.SendAsync(
                It.Is<ReadOnlyMemory<byte>>(m => m.ToArray().SequenceEqual(qa)),
                WebSocketMessageType.Text,
                true,
                token
            ))
            .Callback(() => sendCalled = true)
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(_httpContextMock.Object, _webSocketMock.Object, options, token);

        // Assert
        Assert.True(sendCalled);
        _webSocketMock.Verify(s => s.SendAsync(
            It.Is<ReadOnlyMemory<byte>>(m => m.ToArray().SequenceEqual(qa)),
            WebSocketMessageType.Text,
            true,
            token
        ), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DisposesBuffer_AfterProcessing()
    {
        // Arrange
        var options = new WsConfiguration(new WsOptions());
        var token = CancellationToken.None;
        var bufferMock = new Mock<IMessageBuffer>();
        var serializerMock = new Mock<ISerializer>();
        var expectedHandshakeOptions = new WsHandshakeOptions();

        // Setup buffer
        var bufferData = "{}"u8.ToArray();
        bufferMock.SetupGet(b => b.Length).Returns(bufferData.Length);
        //bufferMock.SetupGet(b => b.Span).Returns(bufferData.AsSpan());

        // Track disposal
        var disposed = false;
        bufferMock.Setup(b => b.Dispose()).Callback(() => disposed = true);

        var messageBuffer = bufferMock.Object;
        var enumerable = new[] { messageBuffer }.ToAsyncEnumerable();

        _enumeratorMock
            .Setup(e => e.EnumerateAsync(_webSocketMock.Object, options, _objectPoolMock.Object, token))
            .Returns(enumerable);

        _storeMock
            .Setup(s => s.TryGet("json", out It.Ref<ISerializer?>.IsAny))
            .Callback((string protocol, out ISerializer? serializer) =>
            {
                serializer = serializerMock.Object;
            })
            .Returns(true);

        serializerMock
            .Setup(s => s.Deserialize(typeof(WsHandshakeOptions), bufferData))
            .Returns(expectedHandshakeOptions);

        _webSocketMock
            .Setup(s => s.SendAsync(It.IsAny<ReadOnlyMemory<byte>>(), WebSocketMessageType.Text, true, token))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _handler.HandleAsync(_httpContextMock.Object, _webSocketMock.Object, options, token);

        // Assert
        Assert.True(disposed);
        bufferMock.Verify(b => b.Dispose(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ThrowsOperationCanceledException_WhenCancelled()
    {
        // Arrange
        var options = new WsConfiguration(new WsOptions());
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var token = cts.Token;

        // Setup enumerator to throw when cancelled
        _enumeratorMock
            .Setup(e => e.EnumerateAsync(_webSocketMock.Object, options, _objectPoolMock.Object, token))
            .Throws<OperationCanceledException>();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.HandleAsync(_httpContextMock.Object, _webSocketMock.Object, options, token).AsTask()
        );
    }

    [Fact]
    public async Task HandleAsync_ReturnsNull_WhenSerializerReturnsNull()
    {
        // Arrange
        var options = new WsConfiguration(new WsOptions());
        var token = CancellationToken.None;
        var bufferMock = new Mock<IMessageBuffer>();
        var serializerMock = new Mock<ISerializer>();

        // Setup buffer
        var bufferData = "{}"u8.ToArray();
        bufferMock.SetupGet(b => b.Length).Returns(bufferData.Length);
        //bufferMock.SetupGet(b => b.Span).Returns(bufferData.AsSpan());

        var messageBuffer = bufferMock.Object;
        var enumerable = new[] { messageBuffer }.ToAsyncEnumerable();

        _enumeratorMock
            .Setup(e => e.EnumerateAsync(_webSocketMock.Object, options, _objectPoolMock.Object, token))
            .Returns(enumerable);

        ISerializer? serializer = serializerMock.Object;
        _storeMock
            .Setup(s => s.TryGet("json", out serializer))
            .Returns(true);

        serializerMock
            .Setup(s => s.Deserialize(It.IsAny<Type>(), bufferData))
            .Returns<object?>(null);

        // Act
        var result = await _handler.HandleAsync(_httpContextMock.Object, _webSocketMock.Object, options, token);

        // Assert
        Assert.Null(result);
    }
}

public static class TestHelpers
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield(); // Simulate async behavior
        }
    }
}

