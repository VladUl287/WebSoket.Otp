using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Models;

public sealed class WsEndpointInfo
{
    public required Type EndpointType { get; init; }
    public required IEndpointInvoker Invoker { get; init; }
    public Endpoint? AuthEndpoint { get; init; }
}
