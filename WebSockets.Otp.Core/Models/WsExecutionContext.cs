using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Models;

public sealed class WsExecutionContext : IWsExecutionContext
{
    public HttpContext Context => throw new NotImplementedException();

    public string ConnectionId => throw new NotImplementedException();

    public ISerializer Serializer => throw new NotImplementedException();

    public IMessageBuffer Payload => throw new NotImplementedException();

    public ConnectionManager Manager => throw new NotImplementedException();

    public CancellationToken Cancellation => throw new NotImplementedException();
}