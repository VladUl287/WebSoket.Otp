namespace WebSockets.Otp.Abstractions;

public interface IMessageProcessorFactory
{
    IMessageProcessor Create(string name);
}
