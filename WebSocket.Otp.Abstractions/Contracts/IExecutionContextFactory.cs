using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IExecutionContextFactory
{
    IGlobalContext CreateGlobal(HttpContext context, string connectionId, IWsConnectionManager manager);

    IEndpointContext Create(
        IGlobalContext global, IWsConnectionManager manager, IMessageBuffer payload,
        ISerializer serializer, CancellationToken token);
}
