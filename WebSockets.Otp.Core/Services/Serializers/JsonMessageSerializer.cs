using System.Text.Json;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services.Serializers;

public sealed class JsonMessageSerializer : ISerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonMessageSerializer()
    {
        _options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public JsonMessageSerializer(JsonSerializerOptions options) => _options = options;

    public string ProtocolName => "json";

    public ReadOnlyMemory<byte> Serialize<T>(T message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        return JsonSerializer.SerializeToUtf8Bytes(message, _options);
    }

    public object? Deserialize(Type type, ReadOnlySpan<byte> data) =>
        JsonSerializer.Deserialize(data, type, _options);

    public string ExtractField(ReadOnlySpan<byte> field, ReadOnlySpan<byte> data)
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

                return reader.GetString() ?? throw new NullReferenceException();
            }

            reader.Skip();
        }

        throw new NullReferenceException();
    }

    public string ExtractField(ReadOnlySpan<byte> field, ReadOnlySpan<byte> jsonUtf8, IStringPool stringPool)
    {
        var reader = new Utf8JsonReader(jsonUtf8);

        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(field))
            {
                reader.Read();

                if (reader.TokenType is not JsonTokenType.String)
                    break;

                return reader.HasValueSequence ?
                    stringPool.Intern(reader.ValueSequence) :
                    stringPool.Intern(reader.ValueSpan);
            }

            reader.Skip();
        }

        throw new NullReferenceException();
    }
}