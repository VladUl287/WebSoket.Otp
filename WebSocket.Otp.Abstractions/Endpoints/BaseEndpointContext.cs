using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Endpoints;

public abstract class BaseEndpointContext : IEndpointContext
{
    public ISerializer Serializer => throw new NotImplementedException();

    public IMessageBuffer Payload => throw new NotImplementedException();

    public CancellationToken Cancellation => throw new NotImplementedException();

    public HttpContext Context => throw new NotImplementedException();

    public string ConnectionId => throw new NotImplementedException();

    public GroupManager Groups => throw new NotImplementedException();
}
