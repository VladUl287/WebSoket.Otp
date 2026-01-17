using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Services.Serializers;

public sealed class JsonMessageSerializer : ISerializer
{
    private static readonly JsonSerializerOptions _default = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public string Format => "json";

    public ReadOnlyMemory<byte> Serialize<T>(T message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        return JsonSerializer.SerializeToUtf8Bytes(message, _default);
    }

    public object? Deserialize(Type type, ReadOnlySpan<byte> jsonUtf8)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (jsonUtf8.IsEmpty)
            return null;

        return JsonSerializer.Deserialize(jsonUtf8, type, _default);
    }

    public string ExtractField(ReadOnlySpan<byte> field, ReadOnlySpan<byte> jsonUtf8)
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