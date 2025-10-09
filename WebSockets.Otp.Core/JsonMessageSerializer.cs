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

    public string? PeekRoute(ReadOnlyMemory<byte> payload)
    {
        var span = Encoding.UTF8.GetString(payload.Span);
        using var doc = JsonDocument.Parse(span);
        const string route = "route";
        if (doc.RootElement.TryGetProperty(route, out var r))
            return r.GetString();

        return null;
    }

    public ReadOnlyMemory<byte> Serialize<T>(T message) where T : IWsMessage
    {
        var json = JsonSerializer.Serialize(message, Options);
        return Encoding.UTF8.GetBytes(json);
    }

    T? IMessageSerializer.Deserialize<T>(ReadOnlyMemory<byte> payload) where T : class
    {
        var span = Encoding.UTF8.GetString(payload.Span);
        return JsonSerializer.Deserialize<T>(span, Options);
    }
}
