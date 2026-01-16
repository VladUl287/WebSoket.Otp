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

public interface IEndpointContext<TResponse> : IEndpointContext
    where TResponse : notnull
{
    SendManager<TResponse> Send { get; }
}
