using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IEndpointContext : IGlobalContext
{
    IMessageSerializer Serializer { get; }
    ReadOnlyMemory<byte> Payload { get; }
    CancellationToken Cancellation { get; }
}
