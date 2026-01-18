using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class EndpointInvokerFactory : IEndpointInvokerFactory
{
    public IEndpointInvoker Create(Type endpointType) => new EndpointInvoker(endpointType);
}