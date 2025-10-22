using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.AspNet;

public sealed class RequestState : IRequestState<WsConnectionOptions>
{
    private static readonly ConcurrentDictionary<string, WsConnectionOptions> _store = new();

    public string GenerateKey() => Guid.CreateVersion7().ToString();

    public Task Remove(string key)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<WsConnectionOptions> Get(string key)
    {
        if (_store.TryGetValue(key, out var value))
            return Task.FromResult(value);

        return Task.FromResult<WsConnectionOptions>(null);
    }

    public Task Save(string key, WsConnectionOptions state, CancellationToken token)
    {
        _store[key] = state;
        return Task.CompletedTask;
    }
}
