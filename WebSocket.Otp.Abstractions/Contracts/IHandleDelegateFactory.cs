using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Pipeline;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandleDelegateFactory
{
    ExecutionPipeline CreatePipeline(Type endpoint);
    Func<object, IEndpointContext, CancellationToken, Task> CreateHandleDelegate(Type endpointType);
}
