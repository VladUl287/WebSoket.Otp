using WebSockets.Otp.Core.Models;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Contracts.Transport;

namespace WebSockets.Otp.Core.Services;

public sealed class WsConnectionFactory(IIdProvider idProvider) : IWsConnectionFactory
{
    public IWsConnection Create(IWsTransport transport)
    {
        var connectionId = idProvider.Create();
        return new WsConnection(connectionId, transport);
    }
}
