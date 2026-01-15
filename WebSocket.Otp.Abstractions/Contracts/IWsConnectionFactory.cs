using WebSockets.Otp.Abstractions.Contracts.Transport;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnectionFactory
{
    IWsConnection Create(IWsTransport transport);
}
