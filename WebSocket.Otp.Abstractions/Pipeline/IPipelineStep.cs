using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Pipeline;

public interface IPipelineStep
{
    Task ProcessAsync(object endpoint, IEndpointContext context);
}
