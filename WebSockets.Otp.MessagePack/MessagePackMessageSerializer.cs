using MessagePack;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Models;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.MessagePack;

public sealed class MessagePackMessageSerializer(MessagePackSerializerOptions options) : IMessageSerializer
{
    public string Protocol => "message_pack";

    public WebSocketMessageType Type => WebSocketMessageType.Binary;

    public T? Deserialize<T>(ReadOnlyMemory<byte> data) => MessagePackSerializer.Deserialize<T>(data, options);

    public uint ParseUInt(ReadOnlyMemory<byte> data) => new MessagePackReader(data).ReadUInt32();

    private const int KeyField = 0; // "key"
    private const int IdField = 1; // "id"
    private const int ValueField = 2; // "value"
    private const int ErrorField = 3; // "error"

    public void ScanMessage(ReadOnlyMemory<byte> data, Span<Segment> results)
    {
        var reader = new MessagePackReader(data);
        if (reader.NextMessagePackType != MessagePackType.Map)
            return;

        var mapLength = reader.ReadMapHeader();

        for (var i = 0; i < mapLength; i++)
        {
            var fieldKey = reader.ReadInt32();

            var start = (int)reader.Consumed;

            var idx = fieldKey switch
            {
                KeyField => 0,
                IdField => 1,
                ValueField => 2,
                ErrorField => 3,
                _ => -1,
            };

            if (idx >= 0 && !results[idx].Found)
            {
                reader.Skip();
                var end = (int)reader.Consumed;
                results[idx] = new Segment(start, end);
                continue;
            }

            reader.Skip();
        }
    }

    public ReadOnlyMemory<byte> SerializeToMessage<T>(MessageHeaders headers, T data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        var fieldCount = 1;
        if (!string.IsNullOrEmpty(headers.Key)) fieldCount++;
        if (headers.CorrelationId is not null) fieldCount++;

        writer.WriteMapHeader(fieldCount);

        if (!string.IsNullOrEmpty(headers.Key))
        {
            writer.Write(KeyField);
            writer.Write(headers.Key);
        }

        if (headers.CorrelationId is not null)
        {
            writer.Write(IdField);
            writer.Write(headers.CorrelationId.Value);
        }

        switch (headers.Type)
        {
            case MessageType.Error:
                writer.Write(ErrorField);
                MessagePackSerializer.Serialize<T>(ref writer, data, options);
                break;
            default:
                writer.Write(ValueField);
                MessagePackSerializer.Serialize<T>(ref writer, data, options);
                break;
        }

        writer.Flush();
        return buffer.WrittenMemory;
    }
}
