namespace WebSockets.Otp.Abstractions.Endpoints;

public sealed class EndpointContext : BaseEndpointContext
{
    public SendManager Send { get; }
}

public sealed class EndpointContext<TResponse> : BaseEndpointContext
    where TResponse : notnull
{
    public SendManager<TResponse> Send { get; }
}
