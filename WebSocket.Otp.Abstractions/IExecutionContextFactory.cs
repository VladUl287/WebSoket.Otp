using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions
{
    public interface IExecutionContextFactory
    {
        IWsExecutionContext Create(string endpointKey, Type endpointType, IWsConnection connection, ReadOnlyMemory<byte> payload, IMessageSerializer serializer, CancellationToken token);
    }
}
