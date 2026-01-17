using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Pipeline;

namespace WebSockets.Otp.Core.Services;

public sealed class EndpointInvoker(IHandleDelegateFactory delegateFactory) : IEndpointInvoker
{
    private readonly ConcurrentDictionary<Type, ExecutionPipeline> _cache = new();

    public Task InvokeEndpointAsync(object endpoint, IEndpointContext context)
    {
        var endpointType = endpoint.GetType();
        var pipeline = _cache.GetOrAdd(
            endpointType, (key, factory) => factory.CreatePipeline(endpointType), delegateFactory);
        return pipeline.ExecuteAsync(endpoint, context);
    }
}
