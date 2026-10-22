using WebSockets.Otp.Abstractions.Connections;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IEndpointContext : IGlobalContext
{
    uint? CorrelationId { get; }
    IWsConnectionManager Manager { get; }
    ReadOnlyMemory<byte> Payload { get; }
    CancellationToken Cancellation { get; }
    SendManager Send { get; }
}
