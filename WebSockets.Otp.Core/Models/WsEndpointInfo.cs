using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Core.Models;

public sealed class WsEndpointInfo
{
    public required Type EndpointType { get; init; }
    public Endpoint? AuthEndpoint { get; init; }
}
