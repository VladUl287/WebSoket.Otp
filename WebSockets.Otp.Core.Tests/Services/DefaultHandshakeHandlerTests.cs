using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Services;

namespace WebSockets.Otp.Core.Tests.Services;

public class DefaultHandshakeHandlerTests
{
    private readonly Mock<ISerializerStore> _storeMock;
    private readonly Mock<IMessageEnumerator> _enumeratorMock;
    private readonly DefaultHandshakeHandler _handler;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<WebSocket> _webSocketMock;

    public DefaultHandshakeHandlerTests()
    {
        _storeMock = new Mock<ISerializerStore>();
        _enumeratorMock = new Mock<IMessageEnumerator>();

        var logger = NullLogger<DefaultHandshakeHandler>.Instance;

        _handler = new DefaultHandshakeHandler(
            _storeMock.Object,
            _enumeratorMock.Object,
            logger
        );

        _httpContextMock = new Mock<HttpContext>();
        _webSocketMock = new Mock<WebSocket>();
    }

    [Fact]
    public async Task HandleAsync_ReturnsNull_WhenNoHandshakeMessage()
    {
        // Arrange
        var options = new WsOptionsSnapshot(new WsOptions());
        var token = CancellationToken.None;

        var emptyEnumerable = Enumerable.Empty<IMessageBuffer>().ToAsyncEnumerable();

        _enumeratorMock
            .Setup(e => e.EnumerateAsync(_webSocketMock.Object, options, token))
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
        var options = new WsOptionsSnapshot(new WsOptions());
        var token = CancellationToken.None;
        var bufferMock = new Mock<IMessageBuffer>();

        bufferMock.SetupGet(b => b.Length).Returns(10);

        var messageBuffer = bufferMock.Object;
        var enumerable = new[] { messageBuffer }.ToAsyncEnumerable();

        _enumeratorMock
            .Setup(e => e.EnumerateAsync(_webSocketMock.Object, options, token))
            .Returns(enumerable);

        _storeMock
            .Setup(s => s.TryGet("json", out It.Ref<IMessageSerializer?>.IsAny))
            .Returns(false);

        // Act
        var result = await _handler.HandleAsync(_httpContextMock.Object, _webSocketMock.Object, options, token);

        // Assert
        Assert.Null(result);
        _storeMock.Verify(s => s.TryGet("json", out It.Ref<IMessageSerializer?>.IsAny), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ThrowsOperationCanceledException_WhenCancelled()
    {
        // Arrange
        var options = new WsOptionsSnapshot(new WsOptions());
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var token = cts.Token;

        _enumeratorMock
            .Setup(e => e.EnumerateAsync(_webSocketMock.Object, options, token))
            .Throws<OperationCanceledException>();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.HandleAsync(_httpContextMock.Object, _webSocketMock.Object, options, token).AsTask()
        );
    }
}

public static class TestHelpers
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield();
        }
    }
}

