using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core;

public sealed class InMemoryConnectionStateService : IConnectionStateService
{
    private static readonly ConcurrentDictionary<string, WsConnectionOptions> _store = new();

    public ValueTask<string> GenerateTokenId(HttpContext context, WsConnectionOptions options, CancellationToken token = default)
    {
        var tokenId = Guid.CreateVersion7().ToString();
        _store[tokenId] = options;
        return ValueTask.FromResult(tokenId);
    }

    public ValueTask<WsConnectionOptions?> GetAsync(string key, CancellationToken token = default)
    {
        if (_store.TryGetValue(key, out var value))
            return ValueTask.FromResult<WsConnectionOptions?>(value);

        return ValueTask.FromResult<WsConnectionOptions?>(null);
    }

    public ValueTask RevokeAsync(string key, CancellationToken token = default)
    {
        _store.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }
}
