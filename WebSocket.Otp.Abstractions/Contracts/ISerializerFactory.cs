namespace WebSockets.Otp.Abstractions.Contracts;

public interface ISerializerFactory
{
    ISerializer? TryResolve(string format);
}
