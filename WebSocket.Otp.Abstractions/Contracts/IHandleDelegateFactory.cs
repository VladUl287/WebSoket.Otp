using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandleDelegateFactory
{
    Func<object, IEndpointContext, CancellationToken, Task> CreateHandleDelegate(Type endpointType);
}
