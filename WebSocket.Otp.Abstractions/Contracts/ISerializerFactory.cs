namespace WebSockets.Otp.Abstractions.Contracts;

public interface ISerializerFactory
{
    ISerializer Resolve(string format);
}
