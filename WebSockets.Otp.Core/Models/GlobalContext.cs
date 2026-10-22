using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Models;

public sealed class GlobalContext(
    HttpContext httpContext,
    WebSocket socket,
    IWsConnection connection,
    IWsConnectionManager manager) : IGlobalContext
{
    private readonly IWsConnectionManager _connectionManager = manager;

    public HttpContext Context { get; init; } = httpContext;

    public WebSocket Socket { get; init; } = socket;

    public IWsConnection Connection => connection;

    public GroupManager Groups => new(_connectionManager);
}
