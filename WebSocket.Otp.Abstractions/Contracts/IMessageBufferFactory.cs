namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageBufferFactory
{
    IMessageBuffer Create(int capacity);
}
