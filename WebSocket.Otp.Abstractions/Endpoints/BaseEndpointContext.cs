using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Endpoints;

public abstract class BaseEndpointContext : IEndpointContext
{
    protected BaseEndpointContext(
        IGlobalContext globalContext,
        IWsConnectionManager manager,
        ISerializer serializer,
        IMessageBuffer payload,
        CancellationToken cancellation)
    {
        Context = globalContext.Context;
        ConnectionId = globalContext.ConnectionId;
        ConnectionManager = manager;
        Serializer = serializer;
        Payload = payload;
        Cancellation = cancellation;
    }

    protected IWsConnectionManager ConnectionManager { get; init; }

    public HttpContext Context { get; init; }
    public string ConnectionId { get; init; }
    public ISerializer Serializer { get; init; }
    public IMessageBuffer Payload { get; init; }
    public CancellationToken Cancellation { get; init; }

    public GroupManager Groups => new(ConnectionManager);
}
