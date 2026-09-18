using StackExchange.Redis;
using Testcontainers.Redis;

namespace WebSockets.Otp.Redis.Tests;

public sealed class RedisFixture : IAsyncLifetime
{
    private RedisContainer _container = null!;
    public IConnectionMultiplexer Multiplexer { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new RedisBuilder("redis:7-alpine").Build();

        await _container.StartAsync();

        var options = ConfigurationOptions.Parse(_container.GetConnectionString());
        options.AllowAdmin = true;
        Multiplexer = await ConnectionMultiplexer.ConnectAsync(options);
    }

    public async Task DisposeAsync()
    {
        await Multiplexer.DisposeAsync();
        await _container.DisposeAsync();
    }

    public async Task FlushAsync()
    {
        foreach (var ep in Multiplexer.GetEndPoints())
        {
            var server = Multiplexer.GetServer(ep);
            await server.FlushDatabaseAsync();
        }
    }
}

[CollectionDefinition("redis")]
public sealed class RedisCollection : ICollectionFixture<RedisFixture> { }
