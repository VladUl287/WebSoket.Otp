using System.Collections.Concurrent;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Pipeline;
using WebSockets.Otp.Core.Pipeline.Steps;

namespace WebSockets.Otp.Core.Pipeline;

public sealed class PipelineFactory(IHandleDelegateFactory delegateFactory) : IPipelineFactory
{
    private readonly ConcurrentDictionary<Type, ExecutionPipeline> _cache = new();

    public ExecutionPipeline CreatePipeline(Type endpoint)
    {
        return _cache.GetOrAdd(
            endpoint,
            (key, state) =>
            {
                return new ExecutionPipeline(1)
                    .AddStep(new EndpointStep(state.delegateFactory, state.endpoint));
            },
            (delegateFactory, endpoint));


    }
}
