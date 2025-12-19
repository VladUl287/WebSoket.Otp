using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnectionFactory
{
    IWsConnection Create(HttpContext context, WebSocket socket);
}
