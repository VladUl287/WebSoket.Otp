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

    public GroupManager Groups => throw new NotImplementedException();
}

public sealed class WsExecutionContext<TResponse> : IEndpointContext<TResponse>
    where TResponse : notnull
{
    public HttpContext Context => throw new NotImplementedException();

    public string ConnectionId => throw new NotImplementedException();

    public ISerializer Serializer => throw new NotImplementedException();

    public IMessageBuffer Payload => throw new NotImplementedException();

    public CancellationToken Cancellation => throw new NotImplementedException();

    public GroupManager Groups => throw new NotImplementedException();

    public SendManager<TResponse> Send => throw new NotImplementedException();
}