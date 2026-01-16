using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IEndpointContext : IGlobalContext
{
    ISerializer Serializer { get; }
    IMessageBuffer Payload { get; }
    CancellationToken Cancellation { get; }
}
