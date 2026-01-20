using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services.IdProviders;

public sealed class GuidIdProvider : IIdProvider
{
    public string Create() => Guid.NewGuid().ToString();
}
