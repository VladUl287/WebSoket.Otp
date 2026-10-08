using System.Security.Claims;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Abstractions.Endpoints;

public class EndpointContext(
   EndpointHeaders headers,
   IGlobalContext context,
   IWsConnectionManager manager,
   IMessageSerializer serializer,
   ReadOnlyMemory<byte> payload,
   ClaimsPrincipal? user,
   CancellationToken token) : BaseEndpointContext(context, manager, serializer, payload, user, token)
{
    public EndpointHeaders Headers { get; init; }
    public SendManager Send => new(headers, Serializer, ConnectionManager);
}

public class EndpointContext<TResponse>(
    IGlobalContext context,
    IWsConnectionManager manager,
    IMessageSerializer serializer,
    ReadOnlyMemory<byte> payload,
    ClaimsPrincipal? user,
    CancellationToken token) : BaseEndpointContext(context, manager, serializer, payload, user, token)
    where TResponse : notnull
{
    public SendManager<TResponse> Send => new(ConnectionManager);
}
