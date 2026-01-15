namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandleDelegateFactory
{
    Func<object, IEndpointExecutionContext, CancellationToken, Task> CreateHandleDelegate(Type endpointType);
}
