using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.IdProviders;

public sealed class GuidIdProvider : IIdProvider
{
    public string Create() => Guid.NewGuid().ToString();
}
