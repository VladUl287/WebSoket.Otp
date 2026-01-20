using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Models;

public sealed class WsGlobalContext : IGlobalContext
{
    private readonly IWsConnectionManager _connectionManager;

    public WsGlobalContext(HttpContext httpContext, string connectionId, IWsConnectionManager manager)
    {
        _connectionManager = manager;

        Context = httpContext;
        ConnectionId = connectionId;
    }

    public HttpContext Context { get; init; }

    public string ConnectionId { get; init; }

    public GroupManager Groups => new(_connectionManager);
}
