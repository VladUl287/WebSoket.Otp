using System.Buffers;
using System.Text;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core;

namespace WebSockets.Otp.Tests;

public class JsonMessageSerializerTests
{
    private readonly ISerializer _serializer = new JsonMessageSerializer();

    [Fact]
    public void Constructor_WithNullOptions_UsesDefaultOptions()
    {
        // Arrange & Act
        var serializer = new JsonMessageSerializer(null);

        // Assert
        Assert.Equal("json", serializer.Format);
        Assert.NotNull(serializer);
    }

    [Fact]
    public void Constructor_WithCustomOptions_UsesProvidedOptions()
    {
        // Arrange
        var customOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Act
        var serializer = new JsonMessageSerializer(customOptions);

        // Assert
        Assert.Equal("json", serializer.Format);
        Assert.NotNull(serializer);
    }

    [Fact]
    public void Format_ReturnsJson()
    {
        // Act & Assert
        Assert.Equal("json", _serializer.Format);
    }

    [Fact]
    public void Serialize_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        object? message = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.Serialize(message));
    }

    [Fact]
    public void Serialize_WithValidObject_ReturnsSerializedBytes()
    {
        // Arrange
        var testObject = new TestMessage { Id = 1, Name = "Test" };

        // Act
        var result = _serializer.Serialize(testObject);

        // Assert
        Assert.False(result.IsEmpty);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void Deserialize_TypeWithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type? type = null;
        var data = new ReadOnlyMemory<byte>(new byte[] { 123, 125 }); // "{}"

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.Deserialize(type!, data));
    }

    [Fact]
    public void Deserialize_TypeWithEmptyMemory_ReturnsNull()
    {
        // Arrange
        var type = typeof(TestMessage);
        var emptyData = ReadOnlyMemory<byte>.Empty;

        // Act
        var result = _serializer.Deserialize(type, emptyData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_TypeWithValidJson_ReturnsDeserializedObject()
    {
        // Arrange
        var type = typeof(TestMessage);
        var json = "{\"id\":1,\"name\":\"Test\"}"u8.ToArray();

        // Act
        var result = _serializer.Deserialize(type, json.AsMemory());

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestMessage>(result);
        var message = (TestMessage)result!;
        Assert.Equal(1, message.Id);
        Assert.Equal("Test", message.Name);
    }

    [Fact]
    public void Deserialize_TypeSpanWithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type? type = null;
        var data = new byte[] { 123, 125 }; // "{}"

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.Deserialize(type!, data.AsSpan()));
    }

    [Fact]
    public void Deserialize_TypeSpanWithEmptySpan_ReturnsNull()
    {
        // Arrange
        var type = typeof(TestMessage);
        var emptyData = ReadOnlySpan<byte>.Empty;

        // Act
        var result = _serializer.Deserialize(type, emptyData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_TypeSpanWithValidJson_ReturnsDeserializedObject()
    {
        // Arrange
        var type = typeof(TestMessage);
        var json = "{\"id\":1,\"name\":\"Test\"}"u8.ToArray();

        // Act
        var result = _serializer.Deserialize(type, json.AsSpan());

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestMessage>(result);
        var message = (TestMessage)result!;
        Assert.Equal(1, message.Id);
        Assert.Equal("Test", message.Name);
    }

    [Fact]
    public void ExtractStringField_StringFieldWithNullField_ThrowsArgumentException()
    {
        // Arrange
        var json = "{\"field\":\"value\"}"u8.ToArray();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.ExtractStringField((string)null!, json.AsSpan()));
    }

    [Fact]
    public void ExtractStringField_StringFieldWithEmptyField_ThrowsArgumentException()
    {
        // Arrange
        var json = "{\"field\":\"value\"}"u8.ToArray();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.ExtractStringField("", json));
    }

    [Fact]
    public void ExtractStringField_StringFieldWithExistingField_ReturnsValue()
    {
        // Arrange
        var json = "{\"name\":\"John\",\"age\":30}"u8.ToArray();

        // Act
        var result = _serializer.ExtractStringField("name", json);

        // Assert
        Assert.Equal("John", result);
    }

    [Fact]
    public void ExtractStringField_StringFieldWithNonExistingField_ReturnsNull()
    {
        // Arrange
        var json = "{\"name\":\"John\",\"age\":30}"u8.ToArray();

        // Act
        var result = _serializer.ExtractStringField("nonexistent", json);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractStringField_StringFieldWithNestedField_ReturnsValue()
    {
        // Arrange
        var json = "{\"person\":{\"name\":\"John\"},\"age\":30}"u8.ToArray();

        // Act
        var result = _serializer.ExtractStringField("person", json);

        // Assert
        Assert.Null(result); // Should return null because "person" is an object, not a string
    }

    [Fact]
    public void ExtractStringField_ByteFieldWithEmptyField_ThrowsArgumentException()
    {
        // Arrange
        var json = "{\"field\":\"value\"}"u8.ToArray();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.ExtractStringField(ReadOnlySpan<byte>.Empty, json));
    }

    [Fact]
    public void ExtractStringField_ByteFieldWithExistingField_ReturnsValue()
    {
        // Arrange
        var json = "{\"name\":\"John\",\"age\":30}"u8.ToArray();
        var field = "name"u8.ToArray();

        // Act
        var result = _serializer.ExtractStringField(field, json);

        // Assert
        Assert.Equal("John", result);
    }

    [Fact]
    public void ExtractStringField_ByteFieldWithNonExistingField_ReturnsNull()
    {
        // Arrange
        var json = "{\"name\":\"John\",\"age\":30}"u8.ToArray();
        var field = "nonexistent"u8.ToArray();

        // Act
        var result = _serializer.ExtractStringField(field, json);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractStringField_StringFieldWithStringPoolWithNullField_ThrowsArgumentException()
    {
        // Arrange
        var json = "{\"field\":\"value\"}"u8.ToArray();
        var stringPool = new MockStringPool();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.ExtractStringField((string)null!, json, stringPool));
    }

    [Fact]
    public void ExtractStringField_StringFieldWithStringPoolWithNullStringPool_ThrowsArgumentException()
    {
        // Arrange
        var json = "{\"field\":\"value\"}"u8.ToArray();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.ExtractStringField("field", json, null!));
    }

    [Fact]
    public void ExtractStringField_StringFieldWithStringPoolWithExistingField_ReturnsValue()
    {
        // Arrange
        var json = "{\"name\":\"John\",\"age\":30}"u8.ToArray();
        var stringPool = new MockStringPool();

        // Act
        var result = _serializer.ExtractStringField("name", json, stringPool);

        // Assert
        Assert.Equal("John", result);
        Assert.True(stringPool.InternWasCalled);
    }

    [Fact]
    public void ExtractStringField_ByteFieldWithStringPoolWithEmptyField_ThrowsArgumentException()
    {
        // Arrange
        var json = "{\"field\":\"value\"}"u8.ToArray();
        var stringPool = new MockStringPool();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.ExtractStringField(ReadOnlySpan<byte>.Empty, json, stringPool));
    }

    [Fact]
    public void ExtractStringField_ByteFieldWithStringPoolWithNullStringPool_ThrowsArgumentException()
    {
        // Arrange
        var json = "{\"field\":\"value\"}"u8.ToArray();
        var field = "field"u8.ToArray();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.ExtractStringField(field, json, null!));
    }

    [Fact]
    public void ExtractStringField_ByteFieldWithStringPoolWithExistingField_ReturnsValue()
    {
        // Arrange
        var json = "{\"name\":\"John\",\"age\":30}"u8.ToArray();
        var field = "name"u8.ToArray();
        var stringPool = new MockStringPool();

        // Act
        var result = _serializer.ExtractStringField(field, json, stringPool);

        // Assert
        Assert.Equal("John", result);
        Assert.True(stringPool.InternWasCalled);
    }

    [Fact]
    public void RoundTrip_SerializeThenDeserialize_ReturnsEquivalentObject()
    {
        // Arrange
        var original = new TestMessage { Id = 42, Name = "RoundTrip Test" };

        // Act
        var serialized = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(typeof(TestMessage), serialized) as TestMessage;

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Name, deserialized.Name);
    }

    [Fact]
    public void ExtractStringField_WithComplexJson_ExtractsCorrectField()
    {
        // Arrange
        var json = """
        {
            "id": 123,
            "description": "Test description",
            "active": true,
            "tags": ["tag1", "tag2"]
        }
        """u8.ToArray();

        // Act
        var result = _serializer.ExtractStringField("description", json);

        // Assert
        Assert.Equal("Test description", result);
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class MockStringPool : IStringPool
    {
        public bool InternWasCalled { get; private set; }

        public Encoding Encoding => throw new NotImplementedException();

        public string Intern(ReadOnlySpan<byte> utf8String)
        {
            InternWasCalled = true;
            return System.Text.Encoding.UTF8.GetString(utf8String);
        }

        public string Intern(ReadOnlySequence<byte> utf8String)
        {
            InternWasCalled = true;
            return System.Text.Encoding.UTF8.GetString(utf8String.ToArray());
        }
    }
}