using WebSockets.Otp.Abstractions.Pipeline;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandleDelegateFactory
{
    Func<object, object, Task> CreateHandleDelegate(Type endpointType);
}
