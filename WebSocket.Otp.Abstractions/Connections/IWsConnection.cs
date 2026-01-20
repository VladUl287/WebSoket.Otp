using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Connections;

public interface IWsConnection
{
    string Id { get; }

    IConnectionTransport Transport { get; }
}
