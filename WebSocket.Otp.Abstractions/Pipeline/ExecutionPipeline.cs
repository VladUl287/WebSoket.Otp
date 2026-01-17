using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Pipeline;

public sealed class ExecutionPipeline(int capacity)
{
    private readonly List<IPipelineStep> _steps = new(capacity);

    public ExecutionPipeline AddStep(IPipelineStep step)
    {
        _steps.Add(step);
        return this;
    }

    public async Task ExecuteAsync(object endpoint, IEndpointContext context)
    {
        foreach (var step in _steps)
        {
            context.Cancellation.ThrowIfCancellationRequested();

            await step.ProcessAsync(endpoint, context);
        }
    }
}
