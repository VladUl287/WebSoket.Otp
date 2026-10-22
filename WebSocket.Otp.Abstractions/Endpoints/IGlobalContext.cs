using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Connections;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IGlobalContext
{
    IWsConnection Connection { get; }
    HttpContext Context { get; }
    WebSocket Socket { get; }
    GroupManager Groups { get; }
}
