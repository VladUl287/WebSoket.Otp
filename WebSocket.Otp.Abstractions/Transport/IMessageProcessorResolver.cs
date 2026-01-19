namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageProcessorResolver
{
    bool CanResolve(string mode);

    IMessageProcessor Resolve(string mode);
}
