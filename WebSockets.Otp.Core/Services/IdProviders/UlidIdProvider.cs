using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services.IdProviders;

public sealed class UlidIdProvider : IIdProvider
{
    public string Create() => Ulid.NewUlid().ToString();
}
