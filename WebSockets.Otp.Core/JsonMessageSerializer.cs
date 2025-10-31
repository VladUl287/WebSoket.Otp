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

    public object? Deserialize(Type type, ReadOnlySpan<byte> jsonUtf8)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (jsonUtf8.IsEmpty)
            return null;

        return JsonSerializer.Deserialize(jsonUtf8, type, Options);
    }

    public string? ExtractStringField(string field, ReadOnlySpan<byte> jsonUtf8)
    {
        ArgumentException.ThrowIfNullOrEmpty(field, nameof(field));

        var reader = new Utf8JsonReader(jsonUtf8);
        var keyField = field.AsSpan();
        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(keyField))
            {
                reader.Read();

                if (reader.TokenType is not JsonTokenType.String)
                    break;

                return reader.GetString();
            }

            reader.Skip();
        }
        return null;
    }

    public string? ExtractStringField(string field, ReadOnlySpan<byte> jsonUtf8, IStringPool stringPool)
    {
        ArgumentException.ThrowIfNullOrEmpty(field, nameof(field));
        ArgumentNullException.ThrowIfNull(stringPool, nameof(stringPool));

        var reader = new Utf8JsonReader(jsonUtf8);
        var keyField = field.AsSpan();
        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(keyField))
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
        return null;
    }

    public string? ExtractStringField(ReadOnlySpan<byte> field, ReadOnlySpan<byte> jsonUtf8)
    {
        if (field.IsEmpty)
            throw new ArgumentException("Field argument is empty", nameof(field));
        if (jsonUtf8.IsEmpty)
            throw new ArgumentException("Data argument is empty", nameof(jsonUtf8));

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

                return reader.GetString();
            }

            reader.Skip();
        }
        return null;
    }

    public string? ExtractStringField(ReadOnlySpan<byte> field, ReadOnlySpan<byte> jsonUtf8, IStringPool stringPool)
    {
        if (field.IsEmpty)
            throw new ArgumentException("Field argument is empty", nameof(field));
        if (jsonUtf8.IsEmpty)
            throw new ArgumentException("Data argument is empty", nameof(jsonUtf8));
        ArgumentNullException.ThrowIfNull(stringPool, nameof(stringPool));

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
        return null;
    }
}