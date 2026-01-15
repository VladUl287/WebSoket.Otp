namespace WebSockets.Otp.Abstractions.Contracts;

public interface IExecutionContextFactory
{
    IEndpointExecutionContext Create(
        string endpointKey, Type endpointType, IWsConnection connection,
        IMessageBuffer payload, ISerializer serializer, CancellationToken token);
}
