using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnectionFactory
{
    IWsConnection Create(IWsTransport transport);
}
