using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Security.Claims;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Abstractions.Endpoints;

public sealed class EndpointHeaders
{
    public string? Key { get; set; }
    public uint? CorrelationId { get; set; }
}

public interface IContextFactory
{
    IGlobalContext CreateGlobal(HttpContext context, WebSocket socket, string connectionId, WsOptionsSnapshot options);

    IEndpointContext Create(
        EndpointHeaders headers, IGlobalContext global, ReadOnlyMemory<byte> payload,
        IMessageSerializer serializer, ClaimsPrincipal? user, CancellationToken token);
}
