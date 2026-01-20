using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeService : IHandshakeService
{
    private static readonly byte[] _responseBytes = [0x7B, 0x7D, 0x1e];

    public string Protocol => "json";

    public ReadOnlyMemory<byte> ResponseBytes => _responseBytes;
}
