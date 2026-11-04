using MessagePack;
using System.Buffers;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.MessagePack;

public sealed class MessagePackMessageSerializer(MessagePackSerializerOptions options) : IMessageSerializer
{
    public string Protocol => "messagepack";
    public WebSocketMessageType Type => WebSocketMessageType.Binary;

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
        => MessagePackSerializer.Deserialize<T>(data, options);

    public MessageHeaders ReadHeaders(ReadOnlyMemory<byte> data)
    {
        var reader = new MessagePackReader(data);

        uint id = 0u;
        ReadOnlyMemory<byte> key = default;
        ReadOnlyMemory<byte> value = default;

        if (reader.TryReadArrayHeader(out var count) && count >= 3)
        {
            if (!reader.TryReadNil())
            {
                var start = reader.Consumed;
                reader.Skip();
                var end = reader.Consumed;
                key = data[(int)start..(int)end];
            }

            id = reader.ReadUInt32();

            var valueStart = reader.Consumed;
            reader.Skip();
            var valueEnd = reader.Consumed;
            value = data[(int)valueStart..(int)valueEnd];
        }

        return new(id, key, value);
    }

    public ReadOnlyMemory<byte> SerializeToMessage<T>(MessageEnvelope<T> envelope)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        writer.WriteArrayHeader(3);

        if (!string.IsNullOrEmpty(envelope.Key))
            writer.Write(envelope.Key);
        else
            writer.WriteNil();

        writer.Write(envelope.Id);

        MessagePackSerializer.Serialize(ref writer, envelope.Value, options);

        writer.Flush();
        return buffer.WrittenMemory;
    }
}
