using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Models;

public sealed class WsConnection(string connectionId, IWsTransport transport) : IWsConnection
{
    public string Id => connectionId;

    public IWsTransport Transport => transport;

    public void Dispose() => transport.Dispose();
}
