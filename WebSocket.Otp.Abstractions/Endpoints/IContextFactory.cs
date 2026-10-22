using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Connections;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IContextFactory
{
    IGlobalContext CreateGlobal(IWsConnection connection, HttpContext context, WebSocket socket);

    IEndpointContext Create(uint? correlationId, IGlobalContext global, ReadOnlyMemory<byte> data, CancellationToken token);
}
