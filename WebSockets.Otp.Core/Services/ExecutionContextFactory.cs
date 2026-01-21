using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class ExecutionContextFactory : IContextFactory
{
    public IGlobalContext CreateGlobal(
        HttpContext context, WebSocket socket, string connectionId, IWsConnectionManager manager) =>
        new WsGlobalContext(context, socket, connectionId, manager);

    public IEndpointContext Create(
        IGlobalContext global, IWsConnectionManager manager, IMessageBuffer payload,
        ISerializer serializer, CancellationToken token) =>
        new WsEndpointContext(global, manager, serializer, payload, token);
}
