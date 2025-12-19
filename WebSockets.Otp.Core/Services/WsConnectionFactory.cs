using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class WsConnectionFactory(IIdProvider idProvider) : IWsConnectionFactory
{
    public IWsConnection Create(HttpContext context, WebSocket socket)
    {
        var connectionId = idProvider.Create();
        return new WsConnection(connectionId, context, socket);
    }
}
