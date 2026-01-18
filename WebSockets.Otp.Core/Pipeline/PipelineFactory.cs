using System.Collections.Concurrent;
using WebSockets.Otp.Core.Pipeline.Steps;
using WebSockets.Otp.Abstractions.Pipeline;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Pipeline;

public sealed class PipelineFactory(IEndpointInvokerFactory invokerFactory) : IPipelineFactory
{
    private readonly ConcurrentDictionary<Type, ExecutionPipeline> _cache = new();

    public ExecutionPipeline CreatePipeline(Type endpoint)
    {
        return _cache.GetOrAdd(
            endpoint,
            (key, state) =>
            {
                var invoker = state.invokerFactory.Create(state.endpoint);
                return new ExecutionPipeline(1)
                    .AddStep(new EndpointStep(invoker));
            },
            (invokerFactory, endpoint));
    }
}
