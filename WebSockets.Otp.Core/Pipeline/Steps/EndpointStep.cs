using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Pipeline;

namespace WebSockets.Otp.Core.Pipeline.Steps;

public sealed class EndpointStep(IEndpointInvoker invoker) : IPipelineStep
{
    public Task ProcessAsync(object endpoint, IEndpointContext context) => invoker.Invoke(endpoint, context);
}
