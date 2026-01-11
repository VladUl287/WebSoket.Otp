using Microsoft.AspNetCore.Builder;

namespace WebSockets.Otp.Core.Middlewares;

public sealed class WsEndpointConventionBuilder(IEndpointConventionBuilder innerBuilder) : IEndpointConventionBuilder
{
    private readonly IEndpointConventionBuilder _innerBuilder = innerBuilder;

    public void Add(Action<EndpointBuilder> convention)
    {
        _innerBuilder.Add(convention);
    }

    public void Finally(Action<EndpointBuilder> finalConvention)
    {
        _innerBuilder.Finally(finalConvention);
    }
}
