using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Models;

public sealed class WsExecutionContext : IEndpointContext
{
    public HttpContext Context => throw new NotImplementedException();

    public string ConnectionId => throw new NotImplementedException();

    public ISerializer Serializer => throw new NotImplementedException();

    public IMessageBuffer Payload => throw new NotImplementedException();

    public CancellationToken Cancellation => throw new NotImplementedException();

    public ValueTask AddToGroupAsync(string groupName, string connectionId)
    {
        throw new NotImplementedException();
    }

    public ValueTask RemoveFromGroupAsync(string groupName, string connectionId)
    {
        throw new NotImplementedException();
    }
}