using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class WsConnectionFactory(IIdProvider idProvider) : IWsConnectionFactory
{
    public IWsConnection Create(IWsTransport transport)
    {
        var connectionId = idProvider.Create();
        return new WsConnection(connectionId, transport);
    }
}
