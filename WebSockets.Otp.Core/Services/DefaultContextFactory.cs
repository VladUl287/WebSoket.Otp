using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class DefaultContextFactory(IWsConnectionManager manager) : IContextFactory
{
    public IGlobalContext CreateGlobal(IWsConnection connection, HttpContext context, WebSocket socket) => 
        new GlobalContext(context, socket, connection, manager);

    public IEndpointContext Create(uint? correlationId, IGlobalContext global, ReadOnlyMemory<byte> data, CancellationToken token) => 
        new EndpointContext(correlationId, global, manager, data, token);
}
