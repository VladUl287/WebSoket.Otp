using System.Text;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Services.Serializers;

namespace WebSockets.Otp.Core.Tests.Services.Serializers;

public class JsonMessageSerializerTests
{
    private static JsonMessageSerializer CreateSerializer() =>
        new(new JsonSerializerOptions());

    private static JsonMessageSerializer CreateSerializer(JsonSerializerOptions options) =>
        new(options);

    [Fact]
    public void Protocol_IsJson()
    {
        Assert.Equal("json", CreateSerializer().Protocol);
    }

    [Fact]
    public void Type_IsText()
    {
        Assert.Equal(System.Net.WebSockets.WebSocketMessageType.Text, CreateSerializer().Type);
    }

    [Fact]
    public void Deserialize_SimpleObject_RoundTrips()
    {
        var serializer = CreateSerializer();
        var original = new TestPayload { Name = "abc", Count = 42 };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(original);

        var result = serializer.Deserialize<TestPayload>(bytes);

        Assert.NotNull(result);
        Assert.Equal("abc", result!.Name);
        Assert.Equal(42, result.Count);
    }

    [Fact]
    public void Deserialize_RespectsOptions()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var serializer = CreateSerializer(options);
        var bytes = Encoding.UTF8.GetBytes("""{"name":"x","count":7}""");

        var result = serializer.Deserialize<TestPayload>(bytes);

        Assert.NotNull(result);
        Assert.Equal("x", result!.Name);
        Assert.Equal(7, result.Count);
    }

    [Fact]
    public void Deserialize_EmptyObject_ReturnsDefault()
    {
        var serializer = CreateSerializer();
        var bytes = Encoding.UTF8.GetBytes("{}");

        var result = serializer.Deserialize<TestPayload>(bytes);

        Assert.NotNull(result);
        Assert.Null(result!.Name);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void ReadHeaders_ExtractsIdKeyAndValue()
    {
        var serializer = CreateSerializer();
        var bytes = Encoding.UTF8.GetBytes(
            """{"id":123,"key":"myKey","value":{"a":1}}""");

        var headers = serializer.ReadHeaders(bytes);

        Assert.Equal(123u, headers.Id);
        Assert.Equal("\"myKey\"", Encoding.UTF8.GetString(headers.Key.Span));
        Assert.Equal("""{"a":1}""", Encoding.UTF8.GetString(headers.Value.Span));
    }

    [Fact]
    public void ReadHeaders_OrderIndependent()
    {
        var serializer = CreateSerializer();
        var bytes = Encoding.UTF8.GetBytes(
            """{"value":"v","key":"k","id":9}""");

        var headers = serializer.ReadHeaders(bytes);

        Assert.Equal(9u, headers.Id);
        Assert.Equal("\"k\"", Encoding.UTF8.GetString(headers.Key.Span));
        Assert.Equal("\"v\"", Encoding.UTF8.GetString(headers.Value.Span));
    }

    [Fact]
    public void ReadHeaders_IgnoresNestedProperties()
    {
        var serializer = CreateSerializer();
        var bytes = Encoding.UTF8.GetBytes(
            """{"id":5,"value":{"id":999,"key":"nested","value":"nope"}}""");

        var headers = serializer.ReadHeaders(bytes);

        Assert.Equal(5u, headers.Id);
        Assert.Equal("""{"id":999,"key":"nested","value":"nope"}""",
            Encoding.UTF8.GetString(headers.Value.Span));
        Assert.True(headers.Key.IsEmpty);
    }

    [Fact]
    public void ReadHeaders_NoHeaders_ReturnsDefaults()
    {
        var serializer = CreateSerializer();
        var bytes = Encoding.UTF8.GetBytes("""{"other":"x"}""");

        var headers = serializer.ReadHeaders(bytes);

        Assert.Equal(0u, headers.Id);
        Assert.True(headers.Key.IsEmpty);
        Assert.True(headers.Value.IsEmpty);
    }

    [Fact]
    public void ReadHeaders_EmptyObject_ReturnsDefaults()
    {
        var serializer = CreateSerializer();
        var bytes = Encoding.UTF8.GetBytes("{}");

        var headers = serializer.ReadHeaders(bytes);

        Assert.Equal(0u, headers.Id);
        Assert.True(headers.Key.IsEmpty);
        Assert.True(headers.Value.IsEmpty);
    }

    [Fact]
    public void ReadHeaders_StringValue_IsCapturedIncludingQuotes()
    {
        var serializer = CreateSerializer();
        var bytes = Encoding.UTF8.GetBytes("""{"value":"hello"}""");

        var headers = serializer.ReadHeaders(bytes);

        Assert.Equal("\"hello\"", Encoding.UTF8.GetString(headers.Value.Span));
    }

    [Fact]
    public void ReadHeaders_NumericValue_IsCaptured()
    {
        var serializer = CreateSerializer();
        var bytes = Encoding.UTF8.GetBytes("""{"id":1234567}""");

        var headers = serializer.ReadHeaders(bytes);

        Assert.Equal(1234567u, headers.Id);
    }

    [Fact]
    public void SerializeToMessage_WritesValueProperty()
    {
        var serializer = CreateSerializer();
        var envelope = new MessageEnvelope<TestPayload>
        {
            Value = new TestPayload { Name = "abc", Count = 1 },
        };

        var bytes = serializer.SerializeToMessage(envelope);

        using var doc = JsonDocument.Parse(bytes);
        Assert.True(doc.RootElement.TryGetProperty("value", out var value));
        Assert.Equal("abc", value.GetProperty("Name").GetString());
        Assert.Equal(1, value.GetProperty("Count").GetInt32());
        Assert.False(doc.RootElement.TryGetProperty("key", out _));
        Assert.False(doc.RootElement.TryGetProperty("id", out _));
    }

    [Fact]
    public void SerializeToMessage_IncludesKeyWhenNonEmpty()
    {
        var serializer = CreateSerializer();
        var envelope = new MessageEnvelope<TestPayload>
        {
            Key = "myKey",
            Value = new TestPayload { Name = "x", Count = 0 },
        };

        var bytes = serializer.SerializeToMessage(envelope);

        using var doc = JsonDocument.Parse(bytes);
        Assert.Equal("myKey", doc.RootElement.GetProperty("key").GetString());
    }

    [Fact]
    public void SerializeToMessage_OmitsKeyWhenNull()
    {
        var serializer = CreateSerializer();
        var envelope = new MessageEnvelope<TestPayload>
        {
            Key = null,
            Value = new TestPayload(),
        };

        var bytes = serializer.SerializeToMessage(envelope);

        using var doc = JsonDocument.Parse(bytes);
        Assert.False(doc.RootElement.TryGetProperty("key", out _));
    }

    [Fact]
    public void SerializeToMessage_OmitsKeyWhenEmpty()
    {
        var serializer = CreateSerializer();
        var envelope = new MessageEnvelope<TestPayload>
        {
            Key = "",
            Value = new TestPayload(),
        };

        var bytes = serializer.SerializeToMessage(envelope);

        using var doc = JsonDocument.Parse(bytes);
        Assert.False(doc.RootElement.TryGetProperty("key", out _));
    }

    [Fact]
    public void SerializeToMessage_IncludesIdWhenNonZero()
    {
        var serializer = CreateSerializer();
        var envelope = new MessageEnvelope<TestPayload>
        {
            Id = 42,
            Value = new TestPayload(),
        };

        var bytes = serializer.SerializeToMessage(envelope);

        using var doc = JsonDocument.Parse(bytes);
        Assert.Equal(42, doc.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public void SerializeToMessage_OmitsIdWhenZero()
    {
        var serializer = CreateSerializer();
        var envelope = new MessageEnvelope<TestPayload>
        {
            Id = 0,
            Value = new TestPayload(),
        };

        var bytes = serializer.SerializeToMessage(envelope);

        using var doc = JsonDocument.Parse(bytes);
        Assert.False(doc.RootElement.TryGetProperty("id", out _));
    }

    [Fact]
    public void SerializeToMessage_ErrorType_WritesErrorProperty()
    {
        var serializer = CreateSerializer();
        var envelope = new MessageEnvelope<string>
        {
            Type = MessageType.Error,
            Value = "boom",
        };

        var bytes = serializer.SerializeToMessage(envelope);

        using var doc = JsonDocument.Parse(bytes);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Equal("boom", error.GetString());
        Assert.False(doc.RootElement.TryGetProperty("value", out _));
    }

    [Fact]
    public void SerializeToMessage_RespectsOptions()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var serializer = CreateSerializer(options);
        var envelope = new MessageEnvelope<TestPayload>
        {
            Value = new TestPayload { Name = "x", Count = 3 },
        };

        var bytes = serializer.SerializeToMessage(envelope);

        using var doc = JsonDocument.Parse(bytes);
        var value = doc.RootElement.GetProperty("value");
        Assert.True(value.TryGetProperty("name", out _));
        Assert.True(value.TryGetProperty("count", out _));
    }

    [Fact]
    public void RoundTrip_HeadersMatchEnvelope()
    {
        var serializer = CreateSerializer();
        var envelope = new MessageEnvelope<TestPayload>
        {
            Key = "rpc",
            Id = 77,
            Value = new TestPayload { Name = "abc", Count = 5 },
        };

        var message = serializer.SerializeToMessage(envelope);
        var headers = serializer.ReadHeaders(message);

        Assert.Equal(77u, headers.Id);
        Assert.Equal("\"rpc\"", Encoding.UTF8.GetString(headers.Key.Span));
    }

    [Fact]
    public void RoundTrip_DeserializeValueSucceeds()
    {
        var serializer = CreateSerializer();
        var envelope = new MessageEnvelope<TestPayload>
        {
            Value = new TestPayload { Name = "abc", Count = 5 },
        };

        var message = serializer.SerializeToMessage(envelope);
        var headers = serializer.ReadHeaders(message);
        var payload = serializer.Deserialize<TestPayload>(headers.Value);

        Assert.NotNull(payload);
        Assert.Equal("abc", payload!.Name);
        Assert.Equal(5, payload.Count);
    }

    private sealed class TestPayload
    {
        public string? Name { get; set; }
        public int Count { get; set; }
    }
}