using System.Buffers;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services;

public sealed class HandshakeRequestParser(JsonSerializerOptions jsonOptions) : IHandshakeRequestParser
{
    public ValueTask<WsConnectionOptions> Parse(ReadOnlySequence<byte> data)
    {
        return JsonSerializer.DeserializeAsync<WsConnectionOptions>(new MemoryStream(data.ToArray()), jsonOptions);
    }
}
