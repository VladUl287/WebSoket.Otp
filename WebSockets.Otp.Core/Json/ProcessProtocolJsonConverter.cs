using System.Text.Json;
using System.Text.Json.Serialization;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Json;

public class ProcessProtocolJsonConverter : JsonConverter<ProcessProtocol>
{
    public override ProcessProtocol Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ??
            throw new JsonException("Value cannot be null");

        return new ProcessProtocol(value);
    }

    public override void Write(Utf8JsonWriter writer, ProcessProtocol value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
