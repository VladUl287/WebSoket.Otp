using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Security.Claims;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Abstractions.Endpoints;

public class EndpointContext(
   EndpointHeaders headers,
   IGlobalContext context,
   IWsConnectionManager manager,
   IMessageSerializer serializer,
   ReadOnlyMemory<byte> payload,
   ClaimsPrincipal? user,
   CancellationToken token) : IEndpointContext
{
    public IWsConnectionManager Manager => manager;
    public HttpContext Context => context.Context;
    public ClaimsPrincipal? User => user ?? context.Context.User;
    public WebSocket Socket => context.Socket;
    public string ConnectionId => context.ConnectionId;
    public WsOptionsSnapshot Options => context.Options;
    public IMessageSerializer Serializer => serializer;
    public ReadOnlyMemory<byte> Payload => payload;
    public CancellationToken Cancellation => token;
    public EndpointHeaders Headers => headers;
    public GroupManager Groups => new(Manager);
    public SendManager Send => new(Serializer, Manager);
}
