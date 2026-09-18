using Moq;
using StackExchange.Redis;
using System.Net.WebSockets;
using System.Text.Json;
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

    private static async Task<T?> WaitForAsync<T>(
     Func<Task<T?>> probe,
     TimeSpan timeout,
     TimeSpan? poll = null)
    {
        var deadline = DateTime.UtcNow + timeout;
        var interval = poll ?? TimeSpan.FromMilliseconds(25);
        while (DateTime.UtcNow < deadline)
        {
            var value = await probe();
            if (value is not null) return value;
            await Task.Delay(interval);
        }
        return default;
    }

    private sealed record Payload(string Text, int Number);

    [Fact]
    public async Task SendAsync_ToConnection_InvokesRegisteredLocalHandler()
    {
        await _fx.FlushAsync();
        await using var sut = new RedisConnectionManager(_fx.Multiplexer);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.RegisterLocalHandler("direct", "c1", json =>
        {
            received.TrySetResult(json);
            return ValueTask.CompletedTask;
        });

        await Task.Delay(100); // let SUBSCRIBE propagate

        await sut.SendAsync("c1", new Payload("hello", 42), CancellationToken.None);

        var json = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var env = JsonSerializer.Deserialize<EnvelopeProbe>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(env);
        Assert.Equal("direct", env!.Kind);
        Assert.Single(env.Targets);
        Assert.Equal("c1", env.Targets[0]);
        Assert.Contains("hello", env.PayloadJson);
        Assert.Contains("42", env.PayloadJson);
    }

    [Fact]
    public async Task SendAsync_ToMultipleConnections_TargetsAllOfThem()
    {
        await _fx.FlushAsync();
        await using var sut = new RedisConnectionManager(_fx.Multiplexer);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.RegisterLocalHandler("direct", "c1", json =>
        {
            received.TrySetResult(json);
            return ValueTask.CompletedTask;
        });

        await Task.Delay(100);

        await sut.SendAsync(new[] { "c1", "c2", "c3" }, new Payload("x", 1), CancellationToken.None);

        var json = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var env = JsonSerializer.Deserialize<EnvelopeProbe>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(env);
        Assert.Equal(3, env!.Targets.Length);
        Assert.Contains("c1", env.Targets);
        Assert.Contains("c2", env.Targets);
        Assert.Contains("c3", env.Targets);
    }

    [Fact]
    public async Task SendToGroup_InvokesGroupHandler()
    {
        await _fx.FlushAsync();
        await using var sut = new RedisConnectionManager(_fx.Multiplexer);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.RegisterLocalHandler("group", "room-42", json =>
        {
            received.TrySetResult(json);
            return ValueTask.CompletedTask;
        });

        await Task.Delay(100);

        await sut.SendToGroupAsync("room-42", new Payload("hi room", 7), CancellationToken.None);

        var json = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var env = JsonSerializer.Deserialize<EnvelopeProbe>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(env);
        Assert.Equal("group", env!.Kind);
        Assert.Single(env.Targets);
        Assert.Equal("room-42", env.Targets[0]);
    }

    [Fact]
    public async Task SendAsync_Broadcast_ResolvesAllLiveConnections()
    {
        await _fx.FlushAsync();
        await using var sut = new RedisConnectionManager(_fx.Multiplexer);

        var _mockConnection1 = new Mock<IWsConnection>();
        _mockConnection1.SetupGet(c => c.Id).Returns("test-connection-1");
        _mockConnection1.SetupGet(c => c.Socket).Returns(_mockSocket.Object);
        _mockConnection1.SetupGet(c => c.Serializer).Returns(_mockSerializer.Object);

        var _mockConnection2 = new Mock<IWsConnection>();
        _mockConnection2.SetupGet(c => c.Id).Returns("test-connection-2");
        _mockConnection2.SetupGet(c => c.Socket).Returns(_mockSocket.Object);
        _mockConnection2.SetupGet(c => c.Serializer).Returns(_mockSerializer.Object);

        var _mockConnection3 = new Mock<IWsConnection>();
        _mockConnection3.SetupGet(c => c.Id).Returns("test-connection-3");
        _mockConnection3.SetupGet(c => c.Socket).Returns(_mockSocket.Object);
        _mockConnection3.SetupGet(c => c.Serializer).Returns(_mockSerializer.Object);

        await sut.TryAdd(_mockConnection1.Object, CancellationToken.None);
        await sut.TryAdd(_mockConnection2.Object, CancellationToken.None);
        await sut.TryAdd(_mockConnection3.Object, CancellationToken.None);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.RegisterLocalHandler("direct", "b", json =>
        {
            received.TrySetResult(json);
            return ValueTask.CompletedTask;
        });

        await Task.Delay(100);

        await sut.SendAsync(new Payload("broadcast", 0), CancellationToken.None);

        var json = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var env = JsonSerializer.Deserialize<EnvelopeProbe>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(env);
        Assert.Equal(3, env!.Targets.Length);
        Assert.Contains("a", env.Targets);
        Assert.Contains("b", env.Targets);
        Assert.Contains("c", env.Targets);
    }

    [Fact]
    public async Task SendAsync_WithEmptyTargets_DoesNotPublish()
    {
        await _fx.FlushAsync();
        await using var sut = new RedisConnectionManager(_fx.Multiplexer);

        var sub = _fx.Multiplexer.GetSubscriber();
        int hits = 0;
        await sub.SubscribeAsync(RedisChannel.Literal("ws:pubsub:direct"),
            (_, _) => Interlocked.Increment(ref hits));

        await sut.SendAsync(Array.Empty<string>(), new Payload("nope", 0), CancellationToken.None);
        await Task.Delay(200);

        Assert.Equal(0, hits);
    }

    [Fact]
    public async Task UnregisterLocalHandler_StopsDelivery()
    {
        await _fx.FlushAsync();
        await using var sut = new RedisConnectionManager(_fx.Multiplexer);

        int hits = 0;
        sut.RegisterLocalHandler("direct", "c1", _ =>
        {
            Interlocked.Increment(ref hits);
            return ValueTask.CompletedTask;
        });
        await Task.Delay(100);

        await sut.SendAsync("c1", new Payload("first", 1), CancellationToken.None);
        await WaitForAsync(async () => hits > 0 ? true : (bool?)null, TimeSpan.FromSeconds(2));

        sut.UnregisterLocalHandler("direct", "c1");
        await sut.SendAsync("c1", new Payload("second", 2), CancellationToken.None);
        await Task.Delay(300);

        Assert.Equal(1, hits);
    }

    [Fact]
    public async Task HandlerExceptions_DoNotPropagate()
    {
        await _fx.FlushAsync();
        await using var sut = new RedisConnectionManager(_fx.Multiplexer);

        sut.RegisterLocalHandler("direct", "c1", _ => throw new InvalidOperationException("boom"));
        await Task.Delay(100);

        // Must not throw to caller.
        await sut.SendAsync("c1", new Payload("x", 1), CancellationToken.None);

        // Give the pub/sub callback time to run and swallow.
        await Task.Delay(300);
    }

    private sealed record EnvelopeProbe(string Kind, string[] Targets, string PayloadJson);

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
