using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Logging;

namespace WebSockets.Otp.Core.Services;

public sealed class EndpointInvoker(IHandleDelegateFactory delegateFactory, ILogger<EndpointInvoker> logger) : IEndpointInvoker
{
    private readonly ConcurrentDictionary<Type, Func<object, IEndpointContext, CancellationToken, Task>> _cache = new();

    public async Task InvokeEndpointAsync(object endpointInstance, IEndpointContext ctx, CancellationToken ct)
    {
        var endpointType = endpointInstance.GetType();
        var connectionId = ctx.ConnectionId;

        try
        {
            logger.LogInvokingEndpoint(endpointType.Name, connectionId);

            var invoker = GetOrAddInvoker(endpointType);
            await invoker(endpointInstance, ctx, ct);

            logger.LogInvocationSuccess(endpointType.Name, connectionId);
        }
        catch (Exception ex)
        {
            logger.LogInvocationFailed(ex, endpointType.Name, connectionId);
            throw;
        }
    }

    public Func<object, IEndpointContext, CancellationToken, Task> GetOrAddInvoker(Type endpointType)
    {
        if (_cache.TryGetValue(endpointType, out var cachedInvoker))
        {
            logger.LogCacheHit(endpointType.Name);
            return cachedInvoker;
        }

        logger.LogCacheMiss(endpointType.Name);

        var invoker = delegateFactory.CreateHandleDelegate(endpointType);
        _cache[endpointType] = invoker;
        return invoker;
    }
}
