using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Core.Services.Serializers;

public sealed class JsonMessageSerializer(JsonSerializerOptions options) : IMessageSerializer
{
    public string Protocol => "json";
    public WebSocketMessageType Type => WebSocketMessageType.Text;

    public T? Deserialize<T>(ReadOnlyMemory<byte> data) => JsonSerializer.Deserialize<T>(data.Span, options);

    private static ReadOnlySpan<byte> Key => "key"u8;
    private static ReadOnlySpan<byte> Id => "id"u8;
    private static ReadOnlySpan<byte> Value => "value"u8;

    public MessageHeaders ReadHeaders(ReadOnlyMemory<byte> data)
    {
        var dataSpan = data.Span;
        var reader = new Utf8JsonReader(dataSpan);

        uint id = 0u;
        ReadOnlyMemory<byte> key = default;
        ReadOnlyMemory<byte> value = default;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    if (reader.CurrentDepth != 1)
                        break;

                    if (reader.ValueTextEquals(Key))
                    {
                        reader.Read();
                        var start = reader.TokenStartIndex;
                        reader.Skip();
                        var end = reader.BytesConsumed;
                        key = data.Slice((int)start, (int)end);
                    }
                    else if (reader.ValueTextEquals(Id))
                    {
                        reader.Read();
                        var start = reader.TokenStartIndex;
                        reader.Skip();
                        var end = reader.BytesConsumed;
                        value = data.Slice((int)start, (int)end);
                    }
                    else if (reader.ValueTextEquals(Value))
                    {
                        reader.Read();
                        var start = reader.TokenStartIndex;
                        reader.Skip();
                        var end = reader.BytesConsumed;
                        id = uint.Parse(dataSpan.Slice((int)start, (int)end));
                    }
                    break;
            }
        }

        return new(id, key, value);
    }

    private static readonly JsonWriterOptions WriterOptions = new() { SkipValidation = true };

    public ReadOnlyMemory<byte> SerializeToMessage<T>(MessageEnvelope<T> envelope)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, WriterOptions);

        writer.WriteStartObject();
        if (!string.IsNullOrEmpty(envelope.Key))
            writer.WriteString("key"u8, envelope.Key);

        if (envelope.Id != default)
            writer.WriteNumber("id"u8, envelope.Id);

        writer.WritePropertyName(envelope.Type == MessageType.Error ? "error"u8 : "value"u8);

        JsonSerializer.Serialize(writer, envelope.Value, options);
        writer.WriteEndObject();
        writer.Flush();

        return buffer.WrittenMemory;
    }
}