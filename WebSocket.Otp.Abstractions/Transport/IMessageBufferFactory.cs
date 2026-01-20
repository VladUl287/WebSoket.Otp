namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageBufferFactory
{
    IMessageBuffer Create(int capacity);
}
