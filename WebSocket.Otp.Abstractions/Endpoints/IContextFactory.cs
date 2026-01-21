using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IContextFactory
{
    IGlobalContext CreateGlobal(HttpContext context, WebSocket socket, string connectionId, IWsConnectionManager manager);

    IEndpointContext Create(
        IGlobalContext global, IWsConnectionManager manager, IMessageBuffer payload,
        ISerializer serializer, CancellationToken token);
}
