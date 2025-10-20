using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class WsConnection(string id, HttpContext context, WebSocket socket) : IWsConnection
{
    public string Id => id;

    public HttpContext Context => context;

    public WebSocket Socket => socket;

    public void Dispose() => socket.Dispose();
}
