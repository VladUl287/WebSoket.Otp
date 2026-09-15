using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Endpoints;

public abstract class BaseEndpointContext(
    IGlobalContext context,
    IWsConnectionManager manager,
    ISerializer serializer,
    IMessageBuffer payload,
    CancellationToken cancellation) : IEndpointContext
{
    protected IWsConnectionManager ConnectionManager => manager;
    public HttpContext Context => context.Context;
    public WebSocket Socket => context.Socket;
    public string ConnectionId => context.ConnectionId;
    public WsOptionsSnapshot Options => context.Options;
    public ISerializer Serializer => serializer;
    public IMessageBuffer Payload => payload;
    public CancellationToken Cancellation => cancellation;
    public GroupManager Groups => new(ConnectionManager);
}
