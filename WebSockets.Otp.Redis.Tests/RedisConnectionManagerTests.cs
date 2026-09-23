using Moq;
using StackExchange.Redis;
using System.Net.WebSockets;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Redis.Tests;

[Collection("redis")]
public sealed class RedisConnectionManagerTests : IAsyncLifetime
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

        _mockSerializer
            .Setup(s => s.Serialize(It.IsAny<ReadOnlyMemory<byte>>()))
            .Returns((ReadOnlyMemory<byte> m) => m.ToArray());

        _mockSerializer
            .SetupGet(s => s.Type)
            .Returns(WebSocketMessageType.Text);
    }

    public async Task InitializeAsync()
    {
        var server = _fx.Multiplexer.GetServer(_fx.Multiplexer.GetEndPoints()[0]);
        await server.FlushDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, int timeoutMs = 5000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (await predicate()) return;
            await Task.Delay(25);
        }
        throw new TimeoutException("Condition was not met within timeout.");
    }

    private Mock<IWsConnection> CreateConnection(string id)
    {
        var socket = new Mock<WebSocket>();
        var serializer = new Mock<ISerializer>();
        serializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        serializer
            .Setup(s => s.Serialize(It.IsAny<ReadOnlyMemory<byte>>()))
            .Returns((ReadOnlyMemory<byte> m) => m.ToArray());

        var conn = new Mock<IWsConnection>();
        conn.SetupGet(c => c.Id).Returns(id);
        conn.SetupGet(c => c.Socket).Returns(socket.Object);
        conn.SetupGet(c => c.Serializer).Returns(serializer.Object);
        conn.Setup(c => c.Socket.SendAsync(
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<WebSocketMessageType>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        return conn;
    }

    private sealed record ConnectionHarness(
    Mock<IWsConnection> Connection,
    Mock<WebSocket> Socket,
    Mock<ISerializer> Serializer);

    private ConnectionHarness CreateConnection1(string id, WebSocketMessageType type = WebSocketMessageType.Text)
    {
        var socket = new Mock<WebSocket>();

        socket.Setup(s => s.SendAsync(
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<WebSocketMessageType>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var serializer = new Mock<ISerializer>();
        serializer.SetupGet(s => s.Type).Returns(type);
        serializer
            .Setup(s => s.Serialize(It.IsAny<ReadOnlyMemory<byte>>()))
            .Returns((ReadOnlyMemory<byte> m) => m.ToArray());

        var conn = new Mock<IWsConnection>();
        conn.SetupGet(c => c.Id).Returns(id);
        conn.SetupGet(c => c.Socket).Returns(socket.Object);
        conn.SetupGet(c => c.Serializer).Returns(serializer.Object);

        return new ConnectionHarness(conn, socket, serializer);
    }

    [Fact]
    public void Constructor_NullRedis_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisConnectionManager(null!));
    }

    [Fact]
    public async Task Constructor_Subscribes_To_Both_Channels()
    {
        var mux = _fx.CreateMultiplexer();
        var sut = new RedisConnectionManager(mux);
        try
        {
            var sub = mux.GetSubscriber();

            Assert.NotNull(sub.SubscribedEndpoint(RedisChannel.Literal("ws:pubsub:direct")));
            Assert.NotNull(sub.SubscribedEndpoint(RedisChannel.Literal("ws:pubsub:group")));
        }
        finally
        {
            await sut.DisposeAsync();
            await mux.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAdd_NewConnection_ReturnsTrue_AndPersists()
    {
        await using var sut = NewSut();
        var conn = CreateConnection("c1");

        var result = await sut.TryAdd(conn.Object, CancellationToken.None);

        Assert.True(result);

        var db = _fx.Multiplexer.GetDatabase();
        Assert.True(await db.SetContainsAsync("ws:connections", "c1"));
        Assert.True(await db.KeyExistsAsync("ws:connection:c1"));
        Assert.Equal("c1", (string?)await db.HashGetAsync("ws:connection:c1", "id"));
    }

    [Fact]
    public async Task TryAdd_Duplicate_ReturnsFalse()
    {
        await using var sut = NewSut();
        var conn = CreateConnection("c1");

        Assert.True(await sut.TryAdd(conn.Object, CancellationToken.None));
        Assert.False(await sut.TryAdd(conn.Object, CancellationToken.None));
    }

    [Fact]
    public async Task TryAdd_NullConnection_Throws()
    {
        await using var sut = NewSut();
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await sut.TryAdd(null!, CancellationToken.None));
    }

    [Fact]
    public async Task TryRemove_EmptyId_ReturnsFalse()
    {
        await using var sut = NewSut();
        Assert.False(await sut.TryRemove("", CancellationToken.None));
        Assert.False(await sut.TryRemove(null!, CancellationToken.None));
    }

    [Fact]
    public async Task TryRemove_NotTracked_ReturnsFalse()
    {
        await using var sut = NewSut();
        Assert.False(await sut.TryRemove("ghost", CancellationToken.None));
    }

    [Fact]
    public async Task TryRemove_TrackedConnection_CleansAllKeys()
    {
        await using var sut = NewSut();
        var conn = CreateConnection("c1");
        await sut.TryAdd(conn.Object, CancellationToken.None);

        await sut.AddToGroupAsync("g1", "c1", CancellationToken.None);
        await sut.AddToGroupAsync("g2", "c1", CancellationToken.None);

        var removed = await sut.TryRemove("c1", CancellationToken.None);
        Assert.True(removed);

        var db = _fx.Multiplexer.GetDatabase();
        Assert.False(await db.SetContainsAsync("ws:connections", "c1"));
        Assert.False(await db.KeyExistsAsync("ws:connection:c1"));
        Assert.False(await db.KeyExistsAsync("ws:conn_groups:c1"));
        Assert.False(await db.SetContainsAsync("ws:group:g1", "c1"));
        Assert.False(await db.SetContainsAsync("ws:group:g2", "c1"));
    }

    [Theory]
    [InlineData("", "c1")]
    [InlineData("g1", "")]
    [InlineData(null, "c1")]
    [InlineData("g1", null)]
    public async Task AddToGroupAsync_InvalidArgs_ReturnsFalse(string? group, string? connId)
    {
        await using var sut = NewSut();
        Assert.False(await sut.AddToGroupAsync(group!, connId!, CancellationToken.None));
    }

    [Fact]
    public async Task AddToGroupAsync_PersistsBothDirections()
    {
        await using var sut = NewSut();
        var conn = CreateConnection("c1");
        await sut.TryAdd(conn.Object, CancellationToken.None);

        Assert.True(await sut.AddToGroupAsync("g1", "c1", CancellationToken.None));

        var db = _fx.Multiplexer.GetDatabase();
        Assert.True(await db.SetContainsAsync("ws:group:g1", "c1"));
        Assert.True(await db.SetContainsAsync("ws:conn_groups:c1", "g1"));
    }

    [Fact]
    public async Task AddToGroupAsync_Idempotent()
    {
        await using var sut = NewSut();
        var conn = CreateConnection("c1");
        await sut.TryAdd(conn.Object, CancellationToken.None);

        Assert.True(await sut.AddToGroupAsync("g1", "c1", CancellationToken.None));
        Assert.True(await sut.AddToGroupAsync("g1", "c1", CancellationToken.None));
    }

    [Fact]
    public async Task RemoveFromGroupAsync_RemovesBothDirections()
    {
        await using var sut = NewSut();
        var conn = CreateConnection("c1");
        await sut.TryAdd(conn.Object, CancellationToken.None);
        await sut.AddToGroupAsync("g1", "c1", CancellationToken.None);

        Assert.True(await sut.RemoveFromGroupAsync("g1", "c1", CancellationToken.None));

        var db = _fx.Multiplexer.GetDatabase();
        Assert.False(await db.SetContainsAsync("ws:group:g1", "c1"));
        Assert.False(await db.SetContainsAsync("ws:conn_groups:c1", "g1"));
    }

    [Theory]
    [InlineData("", "c1")]
    [InlineData("g1", "")]
    public async Task RemoveFromGroupAsync_InvalidArgs_ReturnsFalse(string group, string connId)
    {
        await using var sut = NewSut();
        Assert.False(await sut.RemoveFromGroupAsync(group, connId, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_ToConnection_DeliversPayload()
    {
        await using var sut = NewSut();
        var conn = CreateConnection1("c1");
        await sut.TryAdd(conn.Connection.Object, CancellationToken.None);

        await sut.SendAsync("c1", new { Hello = "world" }, CancellationToken.None);

        await WaitUntilAsync(() =>
            Task.FromResult(conn.Socket.Invocations.Any(i =>
                i.Method.Name == nameof(WebSocket.SendAsync))));
    }

    [Fact]
    public async Task SendAsync_EmptyConnectionId_NoOp()
    {
        await using var sut = NewSut();
        await sut.SendAsync("", new { x = 1 }, CancellationToken.None);
        await sut.SendAsync((string)null!, new { x = 1 }, CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_MultipleConnections_DeliversToAll()
    {
        await using var sut = NewSut();
        var c1 = CreateConnection1("c1");
        var c2 = CreateConnection1("c2");
        var c3 = CreateConnection1("c3");

        await sut.TryAdd(c1.Connection.Object, CancellationToken.None);
        await sut.TryAdd(c2.Connection.Object, CancellationToken.None);
        await sut.TryAdd(c3.Connection.Object, CancellationToken.None);

        await sut.SendAsync(["c1", "c2"], new { msg = "hi" }, CancellationToken.None);

        await WaitUntilAsync(() =>
            Task.FromResult(
                c1.Socket.Invocations.Any(i => i.Method.Name == nameof(WebSocket.SendAsync)) &&
                c2.Socket.Invocations.Any(i => i.Method.Name == nameof(WebSocket.SendAsync))));

        Assert.DoesNotContain(c3.Socket.Invocations,
            i => i.Method.Name == nameof(WebSocket.SendAsync));
    }

    [Fact]
    public async Task SendAsync_EmptyConnectionList_NoOp()
    {
        await using var sut = NewSut();
        await sut.SendAsync(Array.Empty<string>(), new { x = 1 }, CancellationToken.None);
    }

    [Fact]
    public async Task SendToGroupAsync_DeliversToGroupMembers()
    {
        await using var sut = NewSut();
        var c1 = CreateConnection1("c1");
        var c2 = CreateConnection1("c2");

        await sut.TryAdd(c1.Connection.Object, CancellationToken.None);
        await sut.TryAdd(c2.Connection.Object, CancellationToken.None);
        await sut.AddToGroupAsync("g1", "c1", CancellationToken.None);
        await sut.AddToGroupAsync("g1", "c2", CancellationToken.None);

        await sut.SendToGroupAsync("g1", new { grp = "hi" }, CancellationToken.None);

        await WaitUntilAsync(() =>
            Task.FromResult(
                c1.Socket.Invocations.Any(i => i.Method.Name == nameof(WebSocket.SendAsync)) &&
                c2.Socket.Invocations.Any(i => i.Method.Name == nameof(WebSocket.SendAsync))));
    }

    [Fact]
    public async Task SendToGroupAsync_EmptyGroup_NoOp()
    {
        await using var sut = NewSut();
        await sut.SendToGroupAsync("", new { x = 1 }, CancellationToken.None);
    }

    [Fact]
    public async Task SendToGroupAsync_MultipleGroups_Delivers()
    {
        await using var sut = NewSut();
        var c1 = CreateConnection1("c1");
        var c2 = CreateConnection1("c2");

        await sut.TryAdd(c1.Connection.Object, CancellationToken.None);
        await sut.TryAdd(c2.Connection.Object, CancellationToken.None);
        await sut.AddToGroupAsync("g1", "c1", CancellationToken.None);
        await sut.AddToGroupAsync("g2", "c2", CancellationToken.None);

        await sut.SendToGroupAsync(["g1", "g2"], new { x = 1 }, CancellationToken.None);

        await WaitUntilAsync(() =>
            Task.FromResult(
                c1.Socket.Invocations.Any(i => i.Method.Name == nameof(WebSocket.SendAsync)) &&
                c2.Socket.Invocations.Any(i => i.Method.Name == nameof(WebSocket.SendAsync))));
    }

    [Fact]
    public async Task SendToGroupAsync_EmptyList_NoOp()
    {
        await using var sut = NewSut();
        await sut.SendToGroupAsync(Array.Empty<string>(), new { x = 1 }, CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_Broadcast_DeliversToAllConnections()
    {
        await using var sut = NewSut();
        var c1 = CreateConnection1("c1");
        var c2 = CreateConnection1("c2");
        var c3 = CreateConnection1("c3");

        await sut.TryAdd(c1.Connection.Object, CancellationToken.None);
        await sut.TryAdd(c2.Connection.Object, CancellationToken.None);
        await sut.TryAdd(c3.Connection.Object, CancellationToken.None);

        await sut.SendAsync(new { broadcast = true }, CancellationToken.None);

        await WaitUntilAsync(() =>
            Task.FromResult(
                c1.Socket.Invocations.Any(i => i.Method.Name == nameof(WebSocket.SendAsync)) &&
                c2.Socket.Invocations.Any(i => i.Method.Name == nameof(WebSocket.SendAsync)) &&
                c3.Socket.Invocations.Any(i => i.Method.Name == nameof(WebSocket.SendAsync))));
    }

    [Fact]
    public async Task Broadcast_NoConnections_NoOp()
    {
        await using var sut = NewSut();
        await sut.SendAsync(new { x = 1 }, CancellationToken.None);
    }

    [Fact]
    public async Task OnPubSubMessage_InvalidJson_Ignored()
    {
        await using var sut = NewSut();

        var sub = _fx.Multiplexer.GetSubscriber();
        await sub.PublishAsync(RedisChannel.Literal("ws:pubsub:direct"), "not-json");
        await Task.Delay(100);
    }

    [Fact]
    public async Task OnPubSubMessage_UnknownType_Ignored()
    {
        await using var sut = NewSut();

        var envelope = new
        {
            Kind = "direct",
            Targets = new[] { "c1" },
            Data = "{}",
            TypeName = "Definitely.Not.A.Real.Type, FakeAsm"
        };
        var json = JsonSerializer.Serialize(envelope);

        var sub = _fx.Multiplexer.GetSubscriber();
        await sub.PublishAsync(RedisChannel.Literal("ws:pubsub:direct"), json);

        await Task.Delay(100);
    }

    [Fact]
    public async Task OnPubSubMessage_KindMismatch_Ignored()
    {
        await using var sut = NewSut();
        var conn = CreateConnection("c1");
        await sut.TryAdd(conn.Object, CancellationToken.None);

        var envelope = new
        {
            Kind = "group",
            Targets = new[] { "c1" },
            Data = "{}",
            TypeName = typeof(string).AssemblyQualifiedName
        };
        var json = JsonSerializer.Serialize(envelope);

        await _fx.Multiplexer.GetSubscriber()
            .PublishAsync(RedisChannel.Literal("ws:pubsub:direct"), json);

        await Task.Delay(150);

        Assert.DoesNotContain(conn.Invocations,
            i => i.Method.Name == nameof(WebSocket.SendAsync));
    }

    [Fact]
    public async Task OnPubSubMessage_TargetNotLocal_Ignored()
    {
        await using var sut = NewSut();
        var conn = CreateConnection("c1");
        await sut.TryAdd(conn.Object, CancellationToken.None);

        await sut.SendAsync("ghost-connection", new { x = 1 }, CancellationToken.None);

        await Task.Delay(150);

        Assert.DoesNotContain(conn.Invocations,
            i => i.Method.Name == nameof(WebSocket.SendAsync));
    }

    [Fact]
    public async Task OnPubSubMessage_SerializerThrows_SkipsSocketSend()
    {
        await using var sut = NewSut();

        var socket = new Mock<WebSocket>();
        var serializer = new Mock<ISerializer>();
        serializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        serializer
            .Setup(s => s.Serialize(It.IsAny<ReadOnlyMemory<byte>>()))
            .Throws(new InvalidOperationException("boom"));

        var conn = new Mock<IWsConnection>();
        conn.SetupGet(c => c.Id).Returns("c1");
        conn.SetupGet(c => c.Socket).Returns(socket.Object);
        conn.SetupGet(c => c.Serializer).Returns(serializer.Object);

        await sut.TryAdd(conn.Object, CancellationToken.None);

        await sut.SendAsync("c1", new { x = 1 }, CancellationToken.None);

        await Task.Delay(200);

        Assert.DoesNotContain(conn.Invocations,
            i => i.Method.Name == nameof(WebSocket.SendAsync));
    }

    [Fact]
    public async Task OnPubSubMessage_SocketSendThrows_Swallowed()
    {
        await using var sut = NewSut();

        var socket = new Mock<WebSocket>();
        socket.Setup(s => s.SendAsync(
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<WebSocketMessageType>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WebSocketException("closed"));

        var serializer = new Mock<ISerializer>();
        serializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        serializer
            .Setup(s => s.Serialize(It.IsAny<ReadOnlyMemory<byte>>()))
            .Returns((ReadOnlyMemory<byte> m) => m.ToArray());

        var conn = new Mock<IWsConnection>();
        conn.SetupGet(c => c.Id).Returns("c1");
        conn.SetupGet(c => c.Socket).Returns(socket.Object);
        conn.SetupGet(c => c.Serializer).Returns(serializer.Object);

        await sut.TryAdd(conn.Object, CancellationToken.None);
        await sut.SendAsync("c1", new { x = 1 }, CancellationToken.None);
        await Task.Delay(200);
    }

    [Fact]
    public async Task OnPubSubMessage_GroupsConnectionsBySerializerType()
    {
        await using var sut = NewSut();

        var textSocket = new Mock<WebSocket>();
        var binarySocket = new Mock<WebSocket>();

        var textSerializer = new Mock<ISerializer>();
        textSerializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Text);
        textSerializer
            .Setup(s => s.Serialize(It.IsAny<ReadOnlyMemory<byte>>()))
            .Returns((ReadOnlyMemory<byte> m) => m.ToArray());

        var binarySerializer = new Mock<ISerializer>();
        binarySerializer.SetupGet(s => s.Type).Returns(WebSocketMessageType.Binary);
        binarySerializer
            .Setup(s => s.Serialize(It.IsAny<ReadOnlyMemory<byte>>()))
            .Returns((ReadOnlyMemory<byte> m) => m.ToArray());

        var cText = new Mock<IWsConnection>();
        cText.SetupGet(c => c.Id).Returns("cText");
        cText.SetupGet(c => c.Socket).Returns(textSocket.Object);
        cText.SetupGet(c => c.Serializer).Returns(textSerializer.Object);

        var cBinary = new Mock<IWsConnection>();
        cBinary.SetupGet(c => c.Id).Returns("cBinary");
        cBinary.SetupGet(c => c.Socket).Returns(binarySocket.Object);
        cBinary.SetupGet(c => c.Serializer).Returns(binarySerializer.Object);

        await sut.TryAdd(cText.Object, CancellationToken.None);
        await sut.TryAdd(cBinary.Object, CancellationToken.None);

        await sut.SendAsync(new[] { "cText", "cBinary" }, new { x = 1 }, CancellationToken.None);

        await WaitUntilAsync(() =>
            Task.FromResult(
                textSocket.Invocations.Any(i => i.Method.Name == nameof(WebSocket.SendAsync)) &&
                binarySocket.Invocations.Any(i => i.Method.Name == nameof(WebSocket.SendAsync))));
    }

    [Fact]
    public async Task DisposeAsync_UnsubscribesFromChannels()
    {
        var mux = _fx.CreateMultiplexer();
        var sut = new RedisConnectionManager(mux);

        try
        {
            var sub = mux.GetSubscriber();

            Assert.NotNull(sub.SubscribedEndpoint(RedisChannel.Literal("ws:pubsub:direct")));
            Assert.NotNull(sub.SubscribedEndpoint(RedisChannel.Literal("ws:pubsub:group")));

            await sut.DisposeAsync();

            Assert.Null(sub.SubscribedEndpoint(RedisChannel.Literal("ws:pubsub:direct")));
            Assert.Null(sub.SubscribedEndpoint(RedisChannel.Literal("ws:pubsub:group")));
        }
        finally
        {
            await mux.DisposeAsync();
        }
    }

    [Fact]
    public async Task DisposeAsync_Idempotent()
    {
        var sut = NewSut();
        await sut.DisposeAsync();
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task SendAsync_DirectMessage_ReachesConnectionOnOtherManager()
    {
        await using var sutA = NewSut();
        await using var sutB = NewSut();

        var connOnB = CreateConnection1("cross-1");

        await sutB.TryAdd(connOnB.Connection.Object, CancellationToken.None);

        await Task.Delay(100);

        await sutA.SendAsync("cross-1", new { hello = "cross" }, CancellationToken.None);

        await WaitUntilAsync(() =>
            Task.FromResult(connOnB.Socket.Invocations.Any(i =>
                i.Method.Name == nameof(WebSocket.SendAsync))));
    }

    [Fact]
    public async Task SendToGroupAsync_ReachesGroupMembersOnOtherManager()
    {
        await using var sutA = NewSut();
        await using var sutB = NewSut();

        var connOnB = CreateConnection1("cross-grp-1");

        await sutB.TryAdd(connOnB.Connection.Object, CancellationToken.None);
        await sutB.AddToGroupAsync("shared-group", "cross-grp-1", CancellationToken.None);

        await Task.Delay(200);

        await sutA.SendToGroupAsync("shared-group", new { x = 1 }, CancellationToken.None);

        await WaitUntilAsync(() =>
            Task.FromResult(connOnB.Socket.Invocations.Any(
                i => i.Method.Name == nameof(WebSocket.SendAsync))));
    }
}
