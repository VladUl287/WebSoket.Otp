using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class UtcClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
