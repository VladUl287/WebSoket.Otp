using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class WsConnectionFactory(IIdProvider idProvider) : IWsConnectionFactory
{
    public IWsConnection Create(HttpContext context, WebSocket socket)
    {
        var connectionId = idProvider.Create();
        return new WsConnection(connectionId, context, socket);
    }
}
