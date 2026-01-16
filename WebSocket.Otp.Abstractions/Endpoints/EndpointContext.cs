namespace WebSockets.Otp.Abstractions.Endpoints;

public abstract class EndpointContext : BaseEndpointContext
{
    public SendManager Send { get; }
}

public abstract class EndpointContext<TResponse> : BaseEndpointContext
    where TResponse : notnull
{
    public SendManager<TResponse> Send { get; }
}
