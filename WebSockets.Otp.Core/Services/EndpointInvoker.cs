using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Services;

public sealed class EndpointInvoker(IHandleDelegateFactory delegateFactory) : IEndpointInvoker
{
    private readonly ConcurrentDictionary<Type, Func<object, IEndpointContext, CancellationToken, Task>> _cache = new();

    public async Task InvokeEndpointAsync(object endpointInstance, IEndpointContext ctx, CancellationToken ct)
    {
        var endpointType = endpointInstance.GetType();
        var invoker = GetOrAddInvoker(endpointType);
        await invoker(endpointInstance, ctx, ct);
    }

    public Func<object, IEndpointContext, CancellationToken, Task> GetOrAddInvoker(Type endpointType)
    {
        return _cache.GetOrAdd(
            endpointType,
            (key, factory) => delegateFactory.CreateHandleDelegate(endpointType),
            delegateFactory);
    }
}
