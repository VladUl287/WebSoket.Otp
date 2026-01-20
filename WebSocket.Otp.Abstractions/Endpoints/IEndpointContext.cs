using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IEndpointContext : IGlobalContext
{
    ISerializer Serializer { get; }
    IMessageBuffer Payload { get; }
    CancellationToken Cancellation { get; }
}
