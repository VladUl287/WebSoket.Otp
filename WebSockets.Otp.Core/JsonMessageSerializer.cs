using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public sealed class JsonMessageSerializer(JsonSerializerOptions? options = null) : ISerializer
{
    private static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    private readonly JsonSerializerOptions Options = options ?? Default;

    private static readonly string _format = string.Intern("json");
    public string Format => _format;

    public object? Deserialize(Type type, ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (payload.IsEmpty)
            return null;

        return JsonSerializer.Deserialize(payload.Span, type, Options);
    }

    public ReadOnlyMemory<byte> Serialize<T>(T message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        return JsonSerializer.SerializeToUtf8Bytes(message, Options);
    }

    public string? ExtractStringField(string field, ReadOnlyMemory<byte> jsonUtf8)
    {
        if (string.IsNullOrEmpty(field))
            throw new ArgumentException("Field name cannot be null or empty", nameof(field));

        var reader = new Utf8JsonReader(jsonUtf8.Span);
        var keyField = field.AsSpan();
        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(keyField))
            {
                reader.Read();
                return reader.GetString();
            }

            reader.Skip();
        }
        return null;
    }

    public object? Deserialize(Type type, ReadOnlySpan<byte> jsonUtf8)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (jsonUtf8.IsEmpty)
            return null;

        return JsonSerializer.Deserialize(jsonUtf8, type, Options);
    }

    public string? ExtractStringField(string field, ReadOnlySpan<byte> jsonUtf8)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(field), "Field name cannot be null or empty");

        var reader = new Utf8JsonReader(jsonUtf8);
        var keyField = field.AsSpan();
        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(keyField))
            {
                reader.Read();
                return reader.GetString();
            }

            reader.Skip();
        }
        return null;
    }

    public string? ExtractStringField(string field, ReadOnlySpan<byte> jsonUtf8, IStringPool stringPool)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(field));
        ArgumentException.ThrowIfNullOrEmpty(nameof(stringPool));

        var reader = new Utf8JsonReader(jsonUtf8);
        var keyField = field.AsSpan();
        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(keyField))
            {
                reader.Read();
                return reader.HasValueSequence ?
                    stringPool.Intern(reader.ValueSequence) :
                    stringPool.Intern(reader.ValueSpan);
            }

            reader.Skip();
        }
        return null;
    }
}
