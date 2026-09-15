using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Models;

public sealed class WsGlobalContext(
    HttpContext httpContext,
    WebSocket socket,
    string connectionId,
    IWsConnectionManager manager,
    WsOptionsSnapshot options) : IGlobalContext
{
    private readonly IWsConnectionManager _connectionManager = manager;

    public HttpContext Context { get; init; } = httpContext;

    public WebSocket Socket { get; init; } = socket;

    public WsOptionsSnapshot Options => options;

    public string ConnectionId { get; init; } = connectionId;

    public GroupManager Groups => new(_connectionManager);

}
