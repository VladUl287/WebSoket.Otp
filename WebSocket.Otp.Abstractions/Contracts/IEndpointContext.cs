using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IGlobalContext
{
    HttpContext Context { get; }
    string ConnectionId { get; }
    ValueTask AddToGroupAsync(string groupName, string connectionId);
    ValueTask RemoveFromGroupAsync(string groupName, string connectionId);
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
    ValueTask SendAsync(string connectionId, TResponse data, CancellationToken token);
}
