using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IContextFactory
{
    IGlobalContext CreateGlobal(HttpContext context, WebSocket socket, string connectionId, WsOptionsSnapshot options);

    IEndpointContext Create(IGlobalContext global, IMessageBuffer payload, ISerializer serializer, CancellationToken token);
}
