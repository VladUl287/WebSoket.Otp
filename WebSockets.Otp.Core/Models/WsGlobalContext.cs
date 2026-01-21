using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Models;

public sealed class WsGlobalContext : IGlobalContext
{
    private readonly IWsConnectionManager _connectionManager;

    public WsGlobalContext(HttpContext httpContext, WebSocket socket, string connectionId, IWsConnectionManager manager)
    {
        _connectionManager = manager;

        Context = httpContext;
        Socket = socket;
        ConnectionId = connectionId;
    }

    public HttpContext Context { get; init; }

    public WebSocket Socket { get; init; }

    public string ConnectionId { get; init; }

    public GroupManager Groups => new(_connectionManager);
}
