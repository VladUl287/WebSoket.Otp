using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeService(JsonSerializerOptions jsonOptions) : IHandshakeService
{
    private static readonly byte[] _responseBytes = [.. JsonSerializer.SerializeToUtf8Bytes(new { }), 0x1e];

    public string ProtocolName => ProcessProtocol.Json;

    public ReadOnlyMemory<byte> SuccessResponseBytes => _responseBytes;

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
