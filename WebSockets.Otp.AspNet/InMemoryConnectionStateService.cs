using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.AspNet;

public sealed class InMemoryConnectionStateService(IClock clock) : IConnectionStateService
{
    private static readonly ConcurrentDictionary<string, WsConnectionOptions> _store = new();

    public Task<string> GenerateTokenId(HttpContext context, WsConnectionOptions options, CancellationToken token = default)
    {
        var tokenId = Guid.CreateVersion7().ToString();
        _store[tokenId] = options;
        return Task.FromResult(tokenId);
    }

    public Task<WsConnectionOptions> GetAsync(string key, CancellationToken token = default)
    {
        if (_store.TryGetValue(key, out var value))
        {
            if (value.CreatedAt.Add(value.LifeTime) <= clock.UtcNow)
            {
                return Task.FromResult(value);
            }
        }

        return Task.FromResult<WsConnectionOptions>(null);
    }

    public Task RevokeAsync(string key, CancellationToken token = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
