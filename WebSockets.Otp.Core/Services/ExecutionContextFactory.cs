using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class ExecutionContextFactory : IExecutionContextFactory
{
    public IEndpointContext Create(
        string endpointKey, Type endpointType, IWsConnection connection,
        IMessageBuffer payload, ISerializer serializer, CancellationToken token)
    {
        return new WsEndpointContext();
    }
}
