using Moq;
using WebSockets.Otp.Core.Services;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Tests.Services;

public class InMemoryConnectionManagerTests
{
    private readonly Mock<IWsConnection> _mockConnection;
    private readonly Mock<WebSocket> _mockSocket;
    private readonly Mock<IMessageSerializer> _mockSerializer;
    private readonly InMemoryConnectionManager _connectionManager;

    public InMemoryConnectionManagerTests()
    {
        _mockSocket = new Mock<WebSocket>();
        _mockSerializer = new Mock<IMessageSerializer>();
        _mockConnection = new Mock<IWsConnection>();

        _mockConnection.SetupGet(c => c.Id).Returns("test-connection-1");
        _mockConnection.SetupGet(c => c.Socket).Returns(_mockSocket.Object);
        _mockConnection.SetupGet(c => c.Serializer).Returns(_mockSerializer.Object);

        _connectionManager = new InMemoryConnectionManager();
    }

    [Fact]
    public async Task TryAdd_NewConnection_ReturnsTrue()
    {
        // Act
        var result = await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TryAdd_DuplicateConnection_ReturnsFalse()
    {
        // Arrange
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
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);

        // Act
        var result = await _connectionManager.TryRemove("test-connection-1", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TryRemove_NonExistentConnection_ReturnsFalse()
    {
        // Act
        var result = await _connectionManager.TryRemove("non-existent", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddToGroupAsync_NewGroupAndConnection_ReturnsTrue()
    {
        // Arrange
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);

        // Act
        var result = await _connectionManager.AddToGroup("group1", "test-connection-1", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AddToGroupAsync_ConnectionNotInStore_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _connectionManager.AddToGroup("group1", "non-existent", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task AddToGroupAsync_DuplicateConnectionInGroup_ReturnsFalse()
    {
        // Arrange
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.AddToGroup("group1", "test-connection-1", CancellationToken.None);

        // Act
        var result = await _connectionManager.AddToGroup("group1", "test-connection-1", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveFromGroupAsync_ExistingConnectionInGroup_ReturnsTrue()
    {
        // Arrange
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.AddToGroup("group1", "test-connection-1", CancellationToken.None);

        // Act
        var result = await _connectionManager.RemoveFromGroup("group1", "test-connection-1", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RemoveFromGroupAsync_NonExistentConnectionInGroup_ReturnsFalse()
    {
        // Arrange
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);

        // Act
        var result = await _connectionManager.RemoveFromGroup("group1", "test-connection-1", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendAsync_WithConnectionId_SendsToCorrectConnection()
    {
        // Arrange
        var serializedData = new ReadOnlyMemory<byte>([1, 2, 3]);
        var headers = new MessageEnvelope<object>(string.Empty, new { Message = "Hello" });

        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        _mockSerializer.Setup(s => s.SerializeToMessage(headers)).Returns(serializedData);
        _mockSerializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        _mockSocket.Setup(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _connectionManager.Send("test-connection-1", headers, CancellationToken.None);

        // Assert
        _mockSerializer.Verify(s => s.SerializeToMessage(headers), Times.Once);
        _mockSocket.Verify(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithConnectionId_NonExistentConnection_ThrowsKeyNotFoundException()
    {
        // Arrange
        var headers = new MessageEnvelope<object>(string.Empty, new { Message = "Hello" });

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _connectionManager.Send("non-existent", headers, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithConnectionsCollection_SendsToAllSpecifiedConnections()
    {
        // Arrange
        var serializedData = new ReadOnlyMemory<byte>([1, 2, 3]);

        var mockConnection2 = CreateMockConnection("test-connection-2", _mockSocket, _mockSerializer);
        var mockConnection3 = CreateMockConnection("test-connection-3", _mockSocket, _mockSerializer);

        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection2.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection3.Object, CancellationToken.None);

        var headers = new MessageEnvelope<object>(string.Empty, new { Message = "Broadcast" });
        _mockSerializer.Setup(s => s.SerializeToMessage(headers)).Returns(serializedData);
        _mockSerializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        _mockSocket.Setup(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _connectionManager.Send(["test-connection-1", "test-connection-2"], headers, CancellationToken.None);

        // Assert
        _mockSerializer.Verify(s => s.SerializeToMessage(headers), Times.Exactly(1));
        _mockSocket.Verify(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendAsync_Default_SendsToAllConnections()
    {
        // Arrange
        var serializedData = new ReadOnlyMemory<byte>([1, 2, 3]);

        var mockConnection2 = CreateMockConnection("test-connection-2", _mockSocket, _mockSerializer);

        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection2.Object, CancellationToken.None);

        var headers = new MessageEnvelope<object>(string.Empty, new { Message = "Broadcast" });
        _mockSerializer.Setup(s => s.SerializeToMessage(headers)).Returns(serializedData);
        _mockSerializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        _mockSocket.Setup(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _connectionManager.Send(headers, CancellationToken.None);

        // Assert
        _mockSerializer.Verify(s => s.SerializeToMessage(headers), Times.Exactly(1));
        _mockSocket.Verify(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    public sealed record GroupMessage(string Message);

    [Fact]
    public async Task SendToGroupAsync_SingleGroup_SendsToAllConnectionsInGroup()
    {
        // Arrange
        var testData = new GroupMessage("GroupMessage");
        var headers = new MessageEnvelope<GroupMessage>(string.Empty, testData);
        var serializedData = new ReadOnlyMemory<byte>([1, 2, 3]);

        var mockConnection2 = CreateMockConnection("test-connection-2", _mockSocket, _mockSerializer);

        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection2.Object, CancellationToken.None);
        await _connectionManager.AddToGroup("group1", "test-connection-1", CancellationToken.None);
        await _connectionManager.AddToGroup("group1", "test-connection-2", CancellationToken.None);

        _mockSerializer.Setup(s => s.SerializeToMessage(headers)).Returns(serializedData);
        _mockSerializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        _mockSocket.Setup(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _connectionManager.SendToGroup("group1", headers, CancellationToken.None);

        // Assert
        _mockSerializer.Verify(s => s.SerializeToMessage(headers), Times.Exactly(1));
        _mockSocket.Verify(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendToGroupAsync_NonExistentGroup_ThrowsKeyNotFoundException()
    {
        // Arrange
        var testData = new { Message = "GroupMessage" };
        var headers = new MessageEnvelope<object>(string.Empty, testData);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _connectionManager.SendToGroup("non-existent-group", headers, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendToGroupAsync_MultipleGroups_SendsToAllConnectionsInAllGroups()
    {
        // Arrange
        var testData = new { Message = "MultiGroupMessage" };
        var serializedData = new ReadOnlyMemory<byte>([1, 2, 3]);

        var mockConnection2 = CreateMockConnection("test-connection-2", _mockSocket, _mockSerializer);
        var mockConnection3 = CreateMockConnection("test-connection-3", _mockSocket, _mockSerializer);

        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection2.Object, CancellationToken.None);
        await _connectionManager.TryAdd(mockConnection3.Object, CancellationToken.None);

        await _connectionManager.AddToGroup("group1", "test-connection-1", CancellationToken.None);
        await _connectionManager.AddToGroup("group2", "test-connection-2", CancellationToken.None);
        await _connectionManager.AddToGroup("group2", "test-connection-3", CancellationToken.None);

        var headers = new MessageEnvelope<object>(string.Empty, testData);
        _mockSerializer.Setup(s => s.SerializeToMessage(headers)).Returns(serializedData);
        _mockSerializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        _mockSocket.Setup(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _connectionManager.SendToGroup(["group1", "group2"], headers, CancellationToken.None);

        // Assert
        _mockSerializer.Verify(s => s.SerializeToMessage(headers), Times.Exactly(1));
        _mockSocket.Verify(s => s.SendAsync(serializedData, WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleMultipleThreads()
    {
        // Arrange
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
                await _connectionManager.AddToGroup(group, connectionId, CancellationToken.None);
                await _connectionManager.SendToGroup(
                    group, new MessageEnvelope<object>(string.Empty, new { Message = "Test" }), CancellationToken.None);
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
        var testData = new { Message = "Test" };
        var headers = new MessageEnvelope<object>(string.Empty, testData);
        var mockConnection2 = CreateMockConnection("test-connection-2");
        await _connectionManager.TryAdd(mockConnection2.Object, CancellationToken.None);

        // Act
        await _connectionManager.Send(Enumerable.Empty<string>(), headers, CancellationToken.None);

        // Assert - Should not send to any connection
        _mockSerializer.Verify(s => s.SerializeToMessage(headers), Times.Never);
    }

    [Fact]
    public async Task SendToGroupAsync_MultipleGroups_EmptyGroupList_DoesNotSend()
    {
        // Arrange
        var testData = new { Message = "Test" };
        var headers = new MessageEnvelope<object>(string.Empty, testData);
        await _connectionManager.TryAdd(_mockConnection.Object, CancellationToken.None);
        await _connectionManager.AddToGroup("group1", "test-connection-1", CancellationToken.None);

        // Act
        await _connectionManager.SendToGroup(Enumerable.Empty<string>(), headers, CancellationToken.None);

        // Assert - Should not send to any connection
        _mockSerializer.Verify(s => s.SerializeToMessage(headers), Times.Never);
    }

    private Mock<IWsConnection> CreateMockConnection(string connectionId)
    {
        var mockSocket = new Mock<WebSocket>();
        var mockSerializer = new Mock<IMessageSerializer>();
        var mockConnection = new Mock<IWsConnection>();

        mockConnection.SetupGet(c => c.Id).Returns(connectionId);
        mockConnection.SetupGet(c => c.Socket).Returns(mockSocket.Object);
        mockConnection.SetupGet(c => c.Serializer).Returns(mockSerializer.Object);

        mockSerializer.Setup(s => s.SerializeToMessage(It.IsAny<MessageEnvelope<object>>()))
            .Returns(new ArraySegment<byte>([1, 2, 3]));
        mockSerializer.SetupGet(s => s.Type)
            .Returns(System.Net.WebSockets.WebSocketMessageType.Text);

        mockSocket.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(),
            It.IsAny<System.Net.WebSockets.WebSocketMessageType>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mockConnection;
    }

    private Mock<IWsConnection> CreateMockConnection(string connectionId, Mock<WebSocket> mockSocket, Mock<IMessageSerializer> mockSerializer)
    {
        var mockConnection = new Mock<IWsConnection>();

        mockConnection.SetupGet(c => c.Id).Returns(connectionId);
        mockConnection.SetupGet(c => c.Socket).Returns(mockSocket.Object);
        mockConnection.SetupGet(c => c.Serializer).Returns(mockSerializer.Object);

        mockSerializer.Setup(s => s.SerializeToMessage(It.IsAny<MessageEnvelope<object>>()))
            .Returns(new ArraySegment<byte>([1, 2, 3]));
        mockSerializer.SetupGet(s => s.Type)
            .Returns(WebSocketMessageType.Text);

        mockSocket.Setup(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(),
            It.IsAny<WebSocketMessageType>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mockConnection;
    }
}
