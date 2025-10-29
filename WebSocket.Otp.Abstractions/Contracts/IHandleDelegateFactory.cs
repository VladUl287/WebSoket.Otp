namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandleDelegateFactory
{
    Func<object, IWsExecutionContext, CancellationToken, Task> CreateHandleDelegate(Type endpointType);
}
