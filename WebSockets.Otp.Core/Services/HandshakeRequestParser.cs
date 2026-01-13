using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeRequestParser(JsonSerializerOptions jsonOptions) : IHandshakeRequestParser
{
    public ValueTask<WsConnectionOptions> Parse(IMessageBuffer data)
    {
        var connectionOptions = JsonSerializer.Deserialize<WsConnectionOptions>(data.Span, jsonOptions) ??
            throw new NullReferenceException("Fail to parse handshake message");

        return new ValueTask<WsConnectionOptions>(connectionOptions);
    }
}
