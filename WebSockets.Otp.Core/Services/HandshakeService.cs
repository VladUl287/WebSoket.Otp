using System.Text;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeService(JsonSerializerOptions jsonOptions) : IHandshakeService
{
    private static readonly byte[] _responseBytes = [0x7B, 0x7D, 0x1e];

    public string ProtocolName => "json";

    public ReadOnlyMemory<byte> ResponseBytes => _responseBytes;

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
