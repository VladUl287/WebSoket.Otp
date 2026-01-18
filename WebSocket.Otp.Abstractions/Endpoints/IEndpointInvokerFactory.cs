namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IEndpointInvokerFactory
{
    IEndpointInvoker Create(Type endpointType);
}
