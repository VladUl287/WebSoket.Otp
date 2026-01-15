using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class ExecutionContextFactory : IExecutionContextFactory
{
    public IWsExecutionContext Create(
        string endpointKey, Type endpointType, IWsConnection connection,
        IMessageBuffer payload, ISerializer serializer, CancellationToken token)
    {
        return new WsExecutionContext();
    }
}
