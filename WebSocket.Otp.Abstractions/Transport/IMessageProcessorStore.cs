namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageProcessorStore
{
    bool CanResolve(string mode);

    IMessageProcessor Get(string mode);
}
