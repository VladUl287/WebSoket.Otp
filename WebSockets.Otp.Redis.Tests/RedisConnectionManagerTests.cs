using Moq;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Redis.Tests;

[Collection("redis")]
public class RedisConnectionManagerTests
{
    private readonly Mock<IWsConnection> _mockConnection;
    private readonly Mock<WebSocket> _mockSocket;
    private readonly Mock<ISerializer> _mockSerializer;

    private readonly RedisFixture _fx;

    private RedisConnectionManager NewSut() => new(_fx.Multiplexer);

    public RedisConnectionManagerTests(RedisFixture fx)
    {
        _fx = fx;

        _mockSocket = new Mock<WebSocket>();
        _mockSerializer = new Mock<ISerializer>();
        _mockConnection = new Mock<IWsConnection>();

        _mockConnection.SetupGet(c => c.Id).Returns("test-connection-1");
        _mockConnection.SetupGet(c => c.Socket).Returns(_mockSocket.Object);
        _mockConnection.SetupGet(c => c.Serializer).Returns(_mockSerializer.Object);
    }

    [Fact]
    public async Task TryAdd_NewConnection_ReturnsTrue()
    {
        //Arrange 
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();

        // Act
        var result = await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TryAdd_DuplicateConnection_ReturnsFalse()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);

        // Act
        var result = await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryRemove_ExistingConnection_ReturnsTrue()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);

        // Act
        var result = await _connectionManager.TryRemove("test-connection-1", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TryRemove_NonExistentConnection_ReturnsFalse()
    {
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();

        // Act
        var result = await _connectionManager.TryRemove("non-existent", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddToGroupAsync_NewGroupAndConnection_ReturnsTrue()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);

        // Act
        var result = await _connectionManager.AddToGroupAsync("group1", "test-connection-1", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AddToGroupAsync_ConnectionNotInStore_ThrowsKeyNotFoundException()
    {
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _connectionManager.AddToGroupAsync("group1", "non-existent", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task AddToGroupAsync_DuplicateConnectionInGroup_ReturnsFalse()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.AddToGroupAsync("group1", "test-connection-1", CancellationToken.None);

        // Act
        var result = await _connectionManager.AddToGroupAsync("group1", "test-connection-1", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveFromGroupAsync_ExistingConnectionInGroup_ReturnsTrue()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.AddToGroupAsync("group1", "test-connection-1", CancellationToken.None);

        // Act
        var result = await _connectionManager.RemoveFromGroupAsync("group1", "test-connection-1", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RemoveFromGroupAsync_NonExistentConnectionInGroup_ReturnsFalse()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);

        // Act
        var result = await _connectionManager.RemoveFromGroupAsync("group1", "test-connection-1", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendAsync_WithConnectionId_SendsToCorrectConnection()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();

        var testData = new { Message = "Hello" };
        var serializedData = new ReadOnlyMemory<byte>([1, 2, 3]);

        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        _mockSerializer.Setup(s => s.Serialize(testData)).Returns(serializedData);
        _mockSerializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        _mockSocket.Setup(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _connectionManager.SendAsync("test-connection-1", testData, CancellationToken.None);

        // Assert
        _mockSerializer.Verify(s => s.Serialize(testData), Times.Once);
        _mockSocket.Verify(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithConnectionId_NonExistentConnection_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();
        var testData = new { Message = "Hello" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _connectionManager.SendAsync("non-existent", testData, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithConnectionsCollection_SendsToAllSpecifiedConnections()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();
        var testData = new { Message = "Broadcast" };
        var serializedData = new ReadOnlyMemory<byte>([1, 2, 3]);

        var mockConnection2 = CreateMockConnection("test-connection-2", _mockSocket, _mockSerializer);
        var mockConnection3 = CreateMockConnection("test-connection-3", _mockSocket, _mockSerializer);

        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection2.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection3.Object, CancellationToken.None);

        _mockSerializer.Setup(s => s.Serialize(testData)).Returns(serializedData);
        _mockSerializer.SetupGet(s => s.Type).Returns(System.Net.WebSockets.WebSocketMessageType.Text);
        _mockSocket.Setup(s => s.SendAsync(serializedData, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _connectionManager.SendAsync(["test-connection-1", "test-connection-2"], testData, CancellationToken.None);

        // Assert - should send to connections 1 and 2, but not 3
        _mockSerializer.Verify(s => s.Serialize(testData), Times.Exactly(2));
        _mockSocket.Verify(s => s.SendAsync(serializedData, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None), Times.Exactly(2));
    }

    [Fact]
    public async Task SendAsync_Default_SendsToAllConnections()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();
        var testData = new { Message = "Broadcast" };
        var serializedData = new ReadOnlyMemory<byte>([1, 2, 3]);

        var mockConnection2 = CreateMockConnection("test-connection-2", _mockSocket, _mockSerializer);

        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection2.Object, CancellationToken.None);

        _mockSerializer.Setup(s => s.Serialize(testData)).Returns(serializedData);
        _mockSerializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        _mockSocket.Setup(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _connectionManager.SendAsync(testData, CancellationToken.None);

        // Assert - should send to all connections
        _mockSerializer.Verify(s => s.Serialize(testData), Times.Exactly(2));
        _mockSocket.Verify(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None), Times.Exactly(2));
    }

    public sealed record GroupMessage(string Message);

    [Fact]
    public async Task SendToGroupAsync_SingleGroup_SendsToAllConnectionsInGroup()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();
        var testData = new GroupMessage("GroupMessage");
        var serializedData = new ReadOnlyMemory<byte>([1, 2, 3]);

        var mockConnection2 = CreateMockConnection("test-connection-2", _mockSocket, _mockSerializer);

        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection2.Object, CancellationToken.None);
        await _connectionManager.AddToGroupAsync("group1", "test-connection-1", CancellationToken.None);
        await _connectionManager.AddToGroupAsync("group1", "test-connection-2", CancellationToken.None);

        _mockSerializer.Setup(s => s.Serialize(testData))
            .Returns(serializedData);
        _mockSerializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        _mockSocket.Setup(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _connectionManager.SendToGroupAsync("group1", testData, CancellationToken.None);

        // Assert
        //_mockSerializer.Verify(s => s.Serialize(testData), Times.Exactly(2));
        _mockSocket.Verify(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None), Times.Exactly(2));
    }

    [Fact]
    public async Task SendToGroupAsync_NonExistentGroup_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();

        var testData = new { Message = "GroupMessage" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _connectionManager.SendToGroupAsync("non-existent-group", testData, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendToGroupAsync_MultipleGroups_SendsToAllConnectionsInAllGroups()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();

        var testData = new { Message = "MultiGroupMessage" };
        var serializedData = new ReadOnlyMemory<byte>([1, 2, 3]);

        var mockConnection2 = CreateMockConnection("test-connection-2", _mockSocket, _mockSerializer);
        var mockConnection3 = CreateMockConnection("test-connection-3", _mockSocket, _mockSerializer);

        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection2.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection3.Object, CancellationToken.None);

        await _connectionManager.AddToGroupAsync("group1", "test-connection-1", CancellationToken.None);
        await _connectionManager.AddToGroupAsync("group2", "test-connection-2", CancellationToken.None);
        await _connectionManager.AddToGroupAsync("group2", "test-connection-3", CancellationToken.None);

        _mockSerializer.Setup(s => s.Serialize(testData)).Returns(serializedData);
        _mockSerializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        _mockSocket.Setup(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _connectionManager.SendToGroupAsync(new[] { "group1", "group2" }, testData, CancellationToken.None);

        // Assert - should send to connection1 (group1) and connections 2 & 3 (group2)
        _mockSerializer.Verify(s => s.Serialize(testData), Times.Exactly(3));
        _mockSocket.Verify(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None), Times.Exactly(3));
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleMultipleThreads()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();

        var tasks = new List<Task>();
        var iterations = 100;

        // Act - run concurrent operations
        for (int i = 0; i < iterations; i++)
        {
            var connectionId = $"connection-{i}";
            var group = $"group-{i % 10}";

            var mockConnection = CreateMockConnection(connectionId);
            tasks.Add(Task.Run(async () =>
            {
                await _connectionManager.TryAdd(mockConnection.Object, CancellationToken.None);
                await _connectionManager.AddToGroupAsync(group, connectionId, CancellationToken.None);
                await _connectionManager.SendToGroupAsync(group, new { Message = "Test" }, CancellationToken.None);
            }));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert - No exceptions should have been thrown
        Assert.True(tasks.All(t => t.IsCompletedSuccessfully));
    }

    [Fact]
    public async Task SendAsync_WithConnectionsCollection_EmptyCollection_DoesNotSend()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();

        var testData = new { Message = "Test" };
        var mockConnection2 = CreateMockConnection("test-connection-2");
        await _connectionManager.TryAdd(mockConnection2.Object, CancellationToken.None);

        // Act
        await _connectionManager.SendAsync(Enumerable.Empty<string>(), testData, CancellationToken.None);

        // Assert - Should not send to any connection
        _mockSerializer.Verify(s => s.Serialize(testData), Times.Never);
    }

    [Fact]
    public async Task SendToGroupAsync_MultipleGroups_EmptyGroupList_DoesNotSend()
    {
        // Arrange
        await _fx.FlushAsync();
        await using var _connectionManager = NewSut();

        var testData = new { Message = "Test" };
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.AddToGroupAsync("group1", "test-connection-1", CancellationToken.None);

        // Act
        await _connectionManager.SendToGroupAsync(Enumerable.Empty<string>(), testData, CancellationToken.None);

        // Assert - Should not send to any connection
        _mockSerializer.Verify(s => s.Serialize(testData), Times.Never);
    }

    private Mock<IWsConnection> CreateMockConnection(string connectionId)
    {
        var mockSocket = new Mock<WebSocket>();
        var mockSerializer = new Mock<ISerializer>();
        var mockConnection = new Mock<IWsConnection>();

        mockConnection.SetupGet(c => c.Id).Returns(connectionId);
        mockConnection.SetupGet(c => c.Socket).Returns(mockSocket.Object);
        mockConnection.SetupGet(c => c.Serializer).Returns(mockSerializer.Object);

        mockSerializer.Setup(s => s.Serialize(It.IsAny<object>()))
            .Returns(new ArraySegment<byte>(new byte[] { 1, 2, 3 }));
        mockSerializer.SetupGet(s => s.Type)
            .Returns(System.Net.WebSockets.WebSocketMessageType.Text);

        mockSocket.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(),
            It.IsAny<System.Net.WebSockets.WebSocketMessageType>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mockConnection;
    }

    private Mock<IWsConnection> CreateMockConnection(string connectionId, Mock<WebSocket> mockSocket, Mock<ISerializer> mockSerializer)
    {
        var mockConnection = new Mock<IWsConnection>();

        mockConnection.SetupGet(c => c.Id).Returns(connectionId);
        mockConnection.SetupGet(c => c.Socket).Returns(mockSocket.Object);
        mockConnection.SetupGet(c => c.Serializer).Returns(mockSerializer.Object);

        mockSerializer.Setup(s => s.Serialize(It.IsAny<object>()))
            .Returns(new ArraySegment<byte>(new byte[] { 1, 2, 3 }));
        mockSerializer.SetupGet(s => s.Type)
            .Returns(System.Net.WebSockets.WebSocketMessageType.Text);

        mockSocket.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(),
            It.IsAny<System.Net.WebSockets.WebSocketMessageType>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mockConnection;
    }
}
