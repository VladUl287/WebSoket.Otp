using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeService
{
    string ProtocolName { get; }

    ReadOnlyMemory<byte> ResponseBytes { get; }

    bool TryParse(IMessageBuffer data, [NotNullWhen(true)] out WsHandshakeOptions? options);
}
