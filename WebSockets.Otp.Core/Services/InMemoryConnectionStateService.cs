using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services;

public sealed class InMemoryConnectionStateService : IStateService
{
    private static readonly ConcurrentDictionary<string, WsConnectionOptions> _store = new();

    public ValueTask<WsConnectionOptions?> Get(string key, CancellationToken token = default)
    {
        if (_store.TryGetValue(key, out var value))
            return ValueTask.FromResult<WsConnectionOptions?>(value);

        return ValueTask.FromResult<WsConnectionOptions?>(null);
    }

    public ValueTask Set(string key, WsConnectionOptions options, CancellationToken token = default)
    {
        _store[key] = options;
        return ValueTask.CompletedTask;
    }

    public ValueTask Delete(string key, CancellationToken token = default)
    {
        _store.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }
}
