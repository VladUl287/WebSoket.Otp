using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IGlobalExecutionContext
{
    HttpContext Context { get; }
    string ConnectionId { get; }
    ConnectionManager Manager { get; }
}

public interface IEndpointExecutionContext : IGlobalExecutionContext
{
    ISerializer Serializer { get; }
    IMessageBuffer Payload { get; }
    CancellationToken Cancellation { get; }
}
