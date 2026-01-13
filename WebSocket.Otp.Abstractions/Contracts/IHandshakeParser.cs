using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeParser
{
    bool TryParse(IMessageBuffer data, [NotNullWhen(true)] out WsConnectionOptions? options);
}
