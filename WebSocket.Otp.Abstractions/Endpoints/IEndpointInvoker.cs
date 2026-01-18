namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IEndpointInvoker
{
    Task Invoke(object endpoint, IEndpointContext context);
}
