using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Pipeline;

public class ExecutionContext
{
    public required object Endpoint { get; init; }
    public required IEndpointContext EndpointContext { get; init; }
    public required CancellationToken Cancellation { get; init; }

    public T Get<T>(string key) where T : notnull => (T)Data[key];
    public void Set<T>(string key, T value) where T : notnull => Data[key] = value;

    public IDictionary<string, object> Data { get; init; } = new ConcurrentDictionary<string, object>();
}

