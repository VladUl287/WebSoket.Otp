using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnection : IDisposable
{
    string Id { get; }

    IWsTransport Transport { get; }
}
