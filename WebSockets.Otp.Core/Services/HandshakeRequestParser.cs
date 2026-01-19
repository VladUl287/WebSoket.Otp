using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeRequestParser(JsonSerializerOptions jsonOptions) : IHandshakeParser
{
    public string ProtocolName => "json";

    public bool TryParse(IMessageBuffer data, [NotNullWhen(true)] out WsHandshakeOptions? options)
    {
        options = null;

        try
        {
            options = JsonSerializer.Deserialize<WsHandshakeOptions>(data.Span, jsonOptions);
            return options is not null;
        }
        catch
        {
            return false;
        }
    }
}
