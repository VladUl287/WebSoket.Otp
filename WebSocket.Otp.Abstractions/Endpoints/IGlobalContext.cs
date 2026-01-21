using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IGlobalContext
{
    HttpContext Context { get; }
    WebSocket Socket { get; }
    string ConnectionId { get; }
    GroupManager Groups { get; }
}
