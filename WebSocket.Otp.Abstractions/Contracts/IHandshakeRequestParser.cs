using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeRequestParser
{
    ValueTask<WsConnectionOptions> Parse(IMessageBuffer data);
}
