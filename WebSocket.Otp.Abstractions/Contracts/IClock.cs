namespace WebSockets.Otp.Abstractions.Contracts;

public interface IClock
{
    DateTime UtcNow { get; }
}
