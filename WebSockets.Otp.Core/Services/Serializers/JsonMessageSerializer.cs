using System.Text.Json;
using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Core.Services.Serializers;

public sealed class JsonMessageSerializer(JsonSerializerOptions options) : IMessageSerializer
{
    private readonly JsonSerializerOptions _options = options;

    public string Protocol => "json";

    public WebSocketMessageType Type => WebSocketMessageType.Text;

    public ReadOnlyMemory<byte> Serialize<T>(T message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        return JsonSerializer.SerializeToUtf8Bytes(message, _options);
    }

    public T? Deserialize<T>(ReadOnlySpan<byte> data) => JsonSerializer.Deserialize<T>(data, _options);

    public bool TryGetFieldValueIndex(ReadOnlySpan<byte> data, string field, out int index)
    {
        index = 0;

        var reader = new Utf8JsonReader(data);

        while (reader.Read())
        {
            if (reader.TokenType is not JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(field.AsSpan()))
            {
                reader.Read();

                var len = reader.HasValueSequence ? (int)reader.ValueSequence.Length : reader.ValueSpan.Length;
                index = (int)(reader.BytesConsumed - len - 1);
                return true;
            }

            reader.Skip();
        }

        return false;
    }

    public void ScanMessage(ReadOnlySpan<byte> json, string[] fields, Span<JsonSlice> results)
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
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (reader.ValueTextEquals(fields[i]))
                        {
                            idx = i;
                            break;
                        }
                    }

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

    public bool TryGetFieldValueRange(ReadOnlySpan<byte> data, string field, out int start, out int end)
    {
        throw new NotImplementedException();
    }
}