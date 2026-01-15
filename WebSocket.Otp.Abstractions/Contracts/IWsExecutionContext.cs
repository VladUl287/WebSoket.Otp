using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsExecutionContext
{
    HttpContext Context { get; }
    string ConnectionId { get; }

    ISerializer Serializer { get; }
    IMessageBuffer Payload { get; }

    CancellationToken Cancellation { get; }

    ConnectionManager Manager { get; }
}
