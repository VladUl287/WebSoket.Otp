using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeParser
{
    string ProtocolName { get; }

    bool TryParse(IMessageBuffer data, [NotNullWhen(true)] out WsHandshakeOptions? options);
}
