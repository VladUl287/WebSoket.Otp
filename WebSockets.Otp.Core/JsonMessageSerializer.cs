using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Exceptions;

namespace WebSockets.Otp.Core;

public sealed class JsonMessageSerializer(JsonSerializerOptions? options = null) : IMessageSerializer
{
    private static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    private readonly JsonSerializerOptions Options = options ?? Default;

    public object Deserialize(Type type, ReadOnlyMemory<byte> payload)
    {
        try
        {
            var span = Encoding.UTF8.GetString(payload.Span);
            return JsonSerializer.Deserialize(span, type, Options);
        }
        catch (Exception ex)
        {
            throw new MessageSerializationException("Invalid message format", ex);
        }
    }

    public ReadOnlyMemory<byte> Serialize<T>(T message) where T : IWsMessage
    {
        var json = JsonSerializer.Serialize(message, Options);
        return Encoding.UTF8.GetBytes(json);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> payload) where T : class, IWsMessage
    {
        var span = Encoding.UTF8.GetString(payload.Span);
        return JsonSerializer.Deserialize<T>(span, Options);
    }

    public string? ExtractStringField(string field, ReadOnlyMemory<byte> payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            doc.RootElement.TryGetProperty(field, out var element);
            return element.GetString();
        }
        catch { return default; }
    }
}
