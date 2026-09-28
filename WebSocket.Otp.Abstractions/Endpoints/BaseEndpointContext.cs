using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Security.Claims;
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
    ClaimsPrincipal? user,
    CancellationToken token) : IEndpointContext
{
    protected IWsConnectionManager ConnectionManager => manager;
    public HttpContext Context => context.Context;
    public ClaimsPrincipal? User => user ?? context.Context.User;
    public WebSocket Socket => context.Socket;
    public string ConnectionId => context.ConnectionId;
    public WsOptionsSnapshot Options => context.Options;
    public ISerializer Serializer => serializer;
    public IMessageBuffer Payload => payload;
    public CancellationToken Cancellation => token;
    public GroupManager Groups => new(ConnectionManager);
}
