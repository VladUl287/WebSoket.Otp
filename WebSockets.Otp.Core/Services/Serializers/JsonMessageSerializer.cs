using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Core.Services.Serializers;

public sealed class JsonMessageSerializer(JsonSerializerOptions options) : IMessageSerializer
{
    public string Protocol => "json";

    public WebSocketMessageType Type => WebSocketMessageType.Text;

    public ReadOnlyMemory<byte> Serialize<T>(T message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        return JsonSerializer.SerializeToUtf8Bytes(message, options);
    }

    public T? Deserialize<T>(ReadOnlySpan<byte> data) => JsonSerializer.Deserialize<T>(data, options);

    private static ReadOnlySpan<byte> Key => "key"u8;
    private static ReadOnlySpan<byte> CorrelationId => "correlationId"u8;
    private static ReadOnlySpan<byte> Value => "value"u8;

    public void ScanMessage(ReadOnlySpan<byte> json, Span<JsonSlice> results)
    {
        var reader = new Utf8JsonReader(json);
        var found = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    if (reader.CurrentDepth != 1)
                        break;

                    var idx = -1;
                    if (reader.ValueTextEquals(Key)) { idx = 0; }
                    else if (reader.ValueTextEquals(CorrelationId)) { idx = 1; }
                    else if (reader.ValueTextEquals(Value)) { idx = 2; }

                    reader.Read();

                    var start = reader.TokenStartIndex;
                    reader.Skip();
                    var end = reader.BytesConsumed;

                    if (idx >= 0 && !results[idx].Found)
                    {
                        results[idx] = new JsonSlice((int)start, (int)end);
                        found++;
                    }

                    break;
            }
        }
    }

    public ReadOnlyMemory<byte> SerializeToMessage<T>(EndpointHeaders headers, T data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        if (!string.IsNullOrEmpty(headers.Key))
            writer.WriteString("key", headers.Key);

        if (headers.CorrelationId is not null)
            writer.WriteNumber("correlationId", headers.CorrelationId.Value);

        writer.WritePropertyName("value");
        JsonSerializer.Serialize(writer, data, options);
        writer.WriteEndObject();
        writer.Flush();

        return buffer.WrittenMemory;
    }
}