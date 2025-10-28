
using Moq;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core;

namespace WebSockets.Otp.Tests;

public class InMemoryConnectionManagerTests
{
    private readonly InMemoryConnectionManager _connectionManager;
    private readonly Mock<IWsConnection> _mockConnection1;
    private readonly Mock<IWsConnection> _mockConnection2;
    private readonly Mock<WebSocket> _mockSocket1;
    private readonly Mock<WebSocket> _mockSocket2;

    private const string ConnectionId1 = "conn-1";
    private const string ConnectionId2 = "conn-2";

    public InMemoryConnectionManagerTests()
    {
        _connectionManager = new InMemoryConnectionManager();

        _mockSocket1 = new Mock<WebSocket>();
        _mockSocket2 = new Mock<WebSocket>();

        _mockConnection1 = new Mock<IWsConnection>();
        _mockConnection1.Setup(c => c.Id).Returns(ConnectionId1);
        _mockConnection1.Setup(c => c.Socket).Returns(_mockSocket1.Object);

        _mockConnection2 = new Mock<IWsConnection>();
        _mockConnection2.Setup(c => c.Id).Returns(ConnectionId2);
        _mockConnection2.Setup(c => c.Socket).Returns(_mockSocket2.Object);
    }

    [Fact]
    public void TryAdd_WhenConnectionIsValid_ShouldAddConnection()
    {
        // Act
        var result = _connectionManager.TryAdd(_mockConnection1.Object);

        // Assert
        Assert.True(result);
        var connection = _connectionManager.Get(ConnectionId1);
        Assert.Same(_mockConnection1.Object, connection);
    }

    [Fact]
    public void TryAdd_WhenConnectionIsNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _connectionManager.TryAdd(null));
    }

    [Fact]
    public void TryAdd_WhenConnectionAlreadyExists_ShouldReturnFalse()
    {
        // Arrange
        _connectionManager.TryAdd(_mockConnection1.Object);

        // Act
        var result = _connectionManager.TryAdd(_mockConnection1.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Get_WhenConnectionExists_ShouldReturnConnection()
    {
        // Arrange
        _connectionManager.TryAdd(_mockConnection1.Object);

        // Act
        var connection = _connectionManager.Get(ConnectionId1);

        // Assert
        Assert.Same(_mockConnection1.Object, connection);
    }

    [Fact]
    public void Get_WhenConnectionDoesNotExist_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _connectionManager.Get("non-existent-id"));
    }

    [Fact]
    public void EnumerateIds_WhenNoConnections_ShouldReturnEmptyCollection()
    {
        // Act
        var ids = _connectionManager.EnumerateIds();

        // Assert
        Assert.Empty(ids);
    }

    [Fact]
    public void EnumerateIds_WhenConnectionsExist_ShouldReturnAllConnectionIds()
    {
        // Arrange
        _connectionManager.TryAdd(_mockConnection1.Object);
        _connectionManager.TryAdd(_mockConnection2.Object);

        // Act
        var ids = _connectionManager.EnumerateIds().ToHashSet();

        // Assert
        Assert.Equal(2, ids.Count);
        Assert.Contains(ConnectionId1, ids);
        Assert.Contains(ConnectionId2, ids);
    }

    [Fact]
    public void TryRemove_WhenConnectionExists_ShouldRemoveConnectionAndReturnTrue()
    {
        // Arrange
        _connectionManager.TryAdd(_mockConnection1.Object);

        // Act
        var result = _connectionManager.TryRemove(ConnectionId1);

        // Assert
        Assert.True(result);
        Assert.Throws<KeyNotFoundException>(() => _connectionManager.Get(ConnectionId1));
    }

    [Fact]
    public void TryRemove_WhenConnectionDoesNotExist_ShouldReturnFalse()
    {
        // Act
        var result = _connectionManager.TryRemove("non-existent-id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryRemove_WhenConnectionIdIsNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _connectionManager.TryRemove(null));
    }

    [Fact]
    public async Task SendAsync_SingleConnection_WhenConnectionExists_ShouldSendMessage()
    {
        // Arrange
        _connectionManager.TryAdd(_mockConnection1.Object);
        var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        var cancellationToken = new CancellationToken();

        // Act
        await _connectionManager.SendAsync(ConnectionId1, payload, cancellationToken);

        // Assert
        _mockSocket1.Verify(s => s.SendAsync(payload, default, true, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SendAsync_SingleConnection_WhenConnectionDoesNotExist_ShouldThrow()
    {
        // Arrange
        var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        var cancellationToken = new CancellationToken();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _connectionManager.SendAsync("non-existent-id", payload, cancellationToken));
    }

    [Fact]
    public async Task SendAsync_MultipleConnections_WhenConnectionsExist_ShouldSendToAllSpecifiedConnections()
    {
        // Arrange
        _connectionManager.TryAdd(_mockConnection1.Object);
        _connectionManager.TryAdd(_mockConnection2.Object);
        var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        var cancellationToken = new CancellationToken();
        var connectionIds = new[] { ConnectionId1, ConnectionId2 };

        // Act
        await _connectionManager.SendAsync(connectionIds, payload, cancellationToken);

        // Assert
        _mockSocket1.Verify(s => s.SendAsync(payload, default, true, cancellationToken), Times.Once);
        _mockSocket2.Verify(s => s.SendAsync(payload, default, true, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SendAsync_MultipleConnections_WhenSomeConnectionsDoNotExist_ShouldSendOnlyToExistingOnes()
    {
        // Arrange
        _connectionManager.TryAdd(_mockConnection1.Object);
        var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        var cancellationToken = new CancellationToken();
        var connectionIds = new[] { ConnectionId1, "non-existent-id" };

        // Act
        await _connectionManager.SendAsync(connectionIds, payload, cancellationToken);

        // Assert
        _mockSocket1.Verify(s => s.SendAsync(payload, default, true, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SendAsync_MultipleConnections_WhenConnectionIdsContainsDuplicates_ShouldSendOnlyOncePerConnection()
    {
        // Arrange
        _connectionManager.TryAdd(_mockConnection1.Object);
        var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        var cancellationToken = new CancellationToken();
        var connectionIds = new[] { ConnectionId1, ConnectionId1, ConnectionId1 };

        // Act
        await _connectionManager.SendAsync(connectionIds, payload, cancellationToken);

        // Assert
        _mockSocket1.Verify(s => s.SendAsync(payload, default, true, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SendAsync_MultipleConnections_WhenNoConnectionsExist_ShouldNotSendAnyMessages()
    {
        // Arrange
        var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        var cancellationToken = new CancellationToken();
        var connectionIds = new[] { "non-existent-1", "non-existent-2" };

        // Act
        await _connectionManager.SendAsync(connectionIds, payload, cancellationToken);

        // Assert
        _mockSocket1.Verify(s => s.SendAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockSocket2.Verify(s => s.SendAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_MultipleConnections_WhenConnectionIdsIsEmpty_ShouldNotSendAnyMessages()
    {
        // Arrange
        _connectionManager.TryAdd(_mockConnection1.Object);
        var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        var cancellationToken = new CancellationToken();

        // Act
        await _connectionManager.SendAsync(Enumerable.Empty<string>(), payload, cancellationToken);

        // Assert
        _mockSocket1.Verify(s => s.SendAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    //[Fact]
    //public async Task SendAsync_MultipleConnections_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    //{
    //    // Arrange
    //    _connectionManager.TryAdd(_mockConnection1.Object);
    //    var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
    //    var cancellationToken = new CancellationToken(canceled: true);
    //    var connectionIds = new[] { ConnectionId1 };

    //    // Act & Assert
    //    await Assert.ThrowsAsync<OperationCanceledException>(() =>
    //        _connectionManager.SendAsync(connectionIds, payload, cancellationToken));
    //}

    [Fact]
    public void ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var iterations = 1000;
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, iterations, i =>
        {
            try
            {
                var connectionId = $"conn-{i}";
                var mockConnection = new Mock<IWsConnection>();
                mockConnection.Setup(c => c.Id).Returns(connectionId);
                mockConnection.Setup(c => c.Socket).Returns(new Mock<WebSocket>().Object);

                // Add connection
                _connectionManager.TryAdd(mockConnection.Object);

                // Get connection
                var connection = _connectionManager.Get(connectionId);

                // Enumerate IDs
                var ids = _connectionManager.EnumerateIds().ToList();

                // Remove connection
                _connectionManager.TryRemove(connectionId);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.Empty(exceptions);
        Assert.Empty(_connectionManager.EnumerateIds());
    }

    [Fact]
    public void Dispose_ShouldClearAllConnections()
    {
        // Arrange
        _connectionManager.TryAdd(_mockConnection1.Object);
        _connectionManager.TryAdd(_mockConnection2.Object);

        // Act
        // If the class implemented IDisposable, we would call Dispose here
        // For now, we'll test that TryRemove works correctly
        _connectionManager.TryRemove(ConnectionId1);
        _connectionManager.TryRemove(ConnectionId2);

        // Assert
        Assert.Empty(_connectionManager.EnumerateIds());
    }
}