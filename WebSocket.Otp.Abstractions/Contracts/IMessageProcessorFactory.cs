namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageProcessorFactory
{
    IMessageProcessor Create(string name);
}
