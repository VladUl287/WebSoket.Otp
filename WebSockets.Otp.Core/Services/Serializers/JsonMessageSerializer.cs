using System.Buffers;
using System.Text.Json;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Models;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services.Serializers;

public sealed class JsonMessageSerializer(JsonSerializerOptions options) : IMessageSerializer
{
    public string Protocol => "json";
    public WebSocketMessageType Type => WebSocketMessageType.Text;

    public T? Deserialize<T>(ReadOnlyMemory<byte> data) => JsonSerializer.Deserialize<T>(data.Span, options);

    private static ReadOnlySpan<byte> Key => "key"u8;
    private static ReadOnlySpan<byte> Id => "id"u8;
    private static ReadOnlySpan<byte> Value => "value"u8;
    private static ReadOnlySpan<byte> Error => "error"u8;

    public void ScanMessage(ReadOnlyMemory<byte> data, Span<Segment> results)
    {
        var reader = new Utf8JsonReader(data.Span);

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    if (reader.CurrentDepth != 1)
                        break;

                    var idx = -1;
                    if (reader.ValueTextEquals(Key)) { idx = 0; }
                    else if (reader.ValueTextEquals(Id)) { idx = 1; }
                    else if (reader.ValueTextEquals(Value)) { idx = 2; }

                    reader.Read();

                    var start = reader.TokenStartIndex;
                    reader.Skip();
                    var end = reader.BytesConsumed;

                    if (idx >= 0 && !results[idx].Found)
                        results[idx] = new Segment((int)start, (int)end);

                    break;
            }
        }
    }

    public ReadOnlyMemory<byte> SerializeToMessage<T>(MessageHeaders headers, T data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        if (!string.IsNullOrEmpty(headers.Key))
            writer.WriteString(nameof(Key), headers.Key);

        if (headers.CorrelationId is not null)
            writer.WriteNumber(nameof(Id), headers.CorrelationId.Value);

        switch (headers.Type)
        {
            case MessageType.Error:
                writer.WritePropertyName(nameof(Error));
                break;
            default:
                writer.WritePropertyName(nameof(Value));
                break;
        }

        JsonSerializer.Serialize(writer, data, options);
        writer.WriteEndObject();
        writer.Flush();

        return buffer.WrittenMemory;
    }

    public uint ParseUInt(ReadOnlyMemory<byte> data) => uint.Parse(data.Span);
}