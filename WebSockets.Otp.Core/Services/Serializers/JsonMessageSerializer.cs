using System.Text.Json;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Core.Services.Serializers;

public sealed class JsonMessageSerializer(JsonSerializerOptions options) : ISerializer
{
    private readonly JsonSerializerOptions _options = options;

    public string ProtocolName => "json";

    public WebSocketMessageType MessageType => WebSocketMessageType.Text;

    public ReadOnlyMemory<byte> Serialize<T>(T message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        return JsonSerializer.SerializeToUtf8Bytes(message, _options);
    }

    public object? Deserialize(Type type, ReadOnlySpan<byte> data) =>
        JsonSerializer.Deserialize(data, type, _options);

    public int FieldValueIndex(ReadOnlySpan<byte> data, ReadOnlySpan<byte> field)
    {
        var reader = new Utf8JsonReader(data);

        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(field))
            {
                reader.Read();

                if (reader.TokenType is not JsonTokenType.String)
                    break;

                var len = reader.HasValueSequence ? (int)reader.ValueSequence.Length : reader.ValueSpan.Length;
                return (int)(reader.BytesConsumed - len - 1);
            }

            reader.Skip();
        }

        throw new NullReferenceException();
    }
}