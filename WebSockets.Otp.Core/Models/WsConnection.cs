using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Models;

public sealed class WsConnection(string connectionId, IConnectionTransport transport) : IWsConnection
{
    public string Id => connectionId;

    public IConnectionTransport Transport => transport;

    public void Dispose() => transport.Dispose();
}
