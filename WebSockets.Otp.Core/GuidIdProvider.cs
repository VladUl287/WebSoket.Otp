using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.AspNet;

public sealed class GuidIdProvider : IIdProvider
{
    public string Create() => Guid.NewGuid().ToString();
}
