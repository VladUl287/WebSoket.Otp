using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IGlobalContext
{
    HttpContext Context { get; }
    WebSocket Socket { get; }
    WsOptionsSnapshot Options { get; }
    string ConnectionId { get; }
    GroupManager Groups { get; }
}
