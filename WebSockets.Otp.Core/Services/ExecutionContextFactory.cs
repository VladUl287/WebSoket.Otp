using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class ExecutionContextFactory : IExecutionContextFactory
{
    public IGlobalContext CreateGlobal(
        HttpContext context, string connectionId, IConnectionManager manager) =>
        new WsGlobalContext(context, connectionId, manager);

    public IEndpointContext Create(
        IGlobalContext global, IConnectionManager manager, IMessageBuffer payload,
        ISerializer serializer, CancellationToken token) =>
        new WsEndpointContext(global, manager, serializer, payload, token);
}
