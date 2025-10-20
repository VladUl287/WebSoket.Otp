using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnection : IDisposable
{
    string Id { get; }

    HttpContext Context { get; }

    WebSocket Socket { get; }
}
