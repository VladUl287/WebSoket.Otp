using System.Collections.Concurrent;
using WebSockets.Otp.Api.Services.Contracts;

namespace WebSockets.Otp.Api.Services;

public sealed class InMemoryUserConnectionMapStorage : IStorage<long>
{
    private static ConcurrentDictionary<long, HashSet<string>> _storage = new();

    public Task Add(long key, string value)
    {
        if (!_storage.TryGetValue(key, out var connections))
        {
            _storage[key] = connections = [];
        }
        connections.Add(value);
        return Task.CompletedTask;
    }

    public Task Delete(long key, string value)
    {
        if (_storage.TryGetValue(key, out var connections))
        {
            connections.Remove(value);
        }
        return Task.CompletedTask;
    }

    public Task Flush()
    {
        _storage.Clear();
        return Task.CompletedTask;
    }

    public Task<string[]> Get(long[] keys)
    {
        var connections = _storage
            .Where(key => keys.Contains(key.Key))
            .SelectMany(c => c.Value)
            .ToArray();
        return Task.FromResult(connections);
    }
}
