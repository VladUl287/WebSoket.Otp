using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core;

public sealed class InMemoryConnectionStateService : IConnectionStateService
{
    private static readonly ConcurrentDictionary<string, ConnectionSettings> _store = new();

    public Task<string> GenerateTokenId(HttpContext context, ConnectionSettings options, CancellationToken token = default)
    {
        var tokenId = Guid.CreateVersion7().ToString();
        _store[tokenId] = options;
        return Task.FromResult(tokenId);
    }

    public Task<ConnectionSettings?> GetAsync(string key, CancellationToken token = default)
    {
        if (_store.TryGetValue(key, out var value))
            return Task.FromResult<ConnectionSettings?>(value);

        return Task.FromResult<ConnectionSettings?>(null);
    }

    public Task RevokeAsync(string key, CancellationToken token = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
