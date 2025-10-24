namespace WebSockets.Otp.Abstractions.Contracts;

public interface ISerializerFactory
{
    ISerializer? Create(string format);
}
