using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services.IdProviders;

public sealed class GuidIdProvider : IIdProvider
{
    public string Create() => Guid.NewGuid().ToString();
}
