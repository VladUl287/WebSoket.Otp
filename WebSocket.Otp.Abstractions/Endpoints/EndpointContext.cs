using System.Security.Claims;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Endpoints;

public class EndpointContext(
   IGlobalContext context,
   IWsConnectionManager manager,
   ISerializer serializer,
   ReadOnlyMemory<byte> payload,
   ClaimsPrincipal? user,
   CancellationToken token) : BaseEndpointContext(context, manager, serializer, payload, user, token)
{
    public SendManager Send => new(ConnectionManager);
}

public class EndpointContext<TResponse>(
    IGlobalContext context,
    IWsConnectionManager manager,
    ISerializer serializer,
     ReadOnlyMemory<byte> payload,
    ClaimsPrincipal? user,
    CancellationToken token) : BaseEndpointContext(context, manager, serializer, payload, user, token)
    where TResponse : notnull
{
    public SendManager<TResponse> Send => new(ConnectionManager);
}
