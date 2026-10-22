using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Core.Models;

public sealed class EndpointContext(
    uint? correlationId,
    IGlobalContext context,
    IWsConnectionManager manager,
    ReadOnlyMemory<byte> payload,
    CancellationToken token) : IEndpointContext
{
    public IWsConnection Connection => context.Connection;
    public HttpContext Context => context.Context;
    public WebSocket Socket => context.Socket;
    public uint? CorrelationId => correlationId;
    public IWsConnectionManager Manager => manager;
    public ReadOnlyMemory<byte> Payload => payload;
    public CancellationToken Cancellation => token;
    public GroupManager Groups => new(Manager);
    public SendManager Send => new(Manager);
}
