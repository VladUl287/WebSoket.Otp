using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Contracts.Transport;

namespace WebSockets.Otp.Core.Models;

public sealed class WsConnection(string id, HttpContext context, IWsTransport transport) : IWsConnection
{
    public string Id => id;

    public HttpContext Context => context;

    public IWsTransport Transport => transport;

    public void Dispose() => transport.Dispose();
}
