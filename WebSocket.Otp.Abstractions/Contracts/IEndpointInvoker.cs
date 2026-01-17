using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IEndpointInvoker
{
    Task Invoke(object endpoint, IEndpointContext context);
}
