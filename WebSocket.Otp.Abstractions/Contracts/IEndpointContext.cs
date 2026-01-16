using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IGlobalContext
{
    HttpContext Context { get; }
    string ConnectionId { get; }
    GroupManager Groups { get; }
}

public interface IEndpointContext : IGlobalContext
{
    ISerializer Serializer { get; }
    IMessageBuffer Payload { get; }
    CancellationToken Cancellation { get; }
}

public abstract class EndpointContextBase : IEndpointContext
{
    public ISerializer Serializer => throw new NotImplementedException();

    public IMessageBuffer Payload => throw new NotImplementedException();

    public CancellationToken Cancellation => throw new NotImplementedException();

    public HttpContext Context => throw new NotImplementedException();

    public string ConnectionId => throw new NotImplementedException();

    public GroupManager Groups => throw new NotImplementedException();
}

public sealed class EndpointContext : EndpointContextBase
{
    public SendManager Send { get; }
}

public sealed class EndpointContext<TResponse> : EndpointContextBase
    where TResponse : notnull
{
    public SendManager<TResponse> Send { get; }
}
