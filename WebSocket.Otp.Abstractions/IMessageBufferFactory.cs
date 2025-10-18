namespace WebSockets.Otp.Abstractions;

public interface IMessageBufferFactory
{
    IMessageBuffer Create(int capacity);
}
