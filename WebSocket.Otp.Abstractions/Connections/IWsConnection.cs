using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Connections;

public interface IWsConnection : IDisposable
{
    string Id { get; }

    IConnectionTransport Transport { get; }
}
