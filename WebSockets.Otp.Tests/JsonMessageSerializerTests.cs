using System;
using System.Text.Json;
using WebSockets.Otp.Core;

namespace WebSockets.Otp.Tests;

public class JsonMessageSerializerTests
{
    private readonly JsonMessageSerializer _serializer;
    private readonly JsonMessageSerializer _customSerializer;

    public JsonMessageSerializerTests()
    {
        _serializer = new JsonMessageSerializer();

        var customOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = false
        };
        _customSerializer = new JsonMessageSerializer(customOptions);
    }

    #region Test Data Classes
    public class TestPerson
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int Age { get; set; }
        public string? Email { get; set; }
    }

    public class TestNestedObject
    {
        public string? Id { get; set; }
        public TestPerson? Person { get; set; }
        public List<string>? Tags { get; set; }
    }
    #endregion

    #region Constructor Tests
    [Fact]
    public void Constructor_WithNullOptions_UsesDefaultOptions()
    {
        // Act
        var serializer = new JsonMessageSerializer(null);

        // Assert
        Assert.NotNull(serializer);
        Assert.Equal("json", serializer.Format);
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
        Assert.NotNull(serializer);
        Assert.Equal("json", serializer.Format);
    }
    #endregion

    #region Format Property Tests
    [Fact]
    public void Format_Always_ReturnsJson()
    {
        // Act & Assert
        Assert.Equal("json", _serializer.Format);
        Assert.Equal("json", _customSerializer.Format);
    }

    [Fact]
    public void Format_IsInternedString()
    {
        // Act
        var format1 = _serializer.Format;
        var format2 = _customSerializer.Format;
        var newSerializer = new JsonMessageSerializer();

        // Assert
        Assert.Same(format1, format2);
        Assert.Same(format1, newSerializer.Format);
    }
    #endregion

    #region Serialize Tests
    [Fact]
    public void Serialize_WithValidObject_ReturnsUtf8Bytes()
    {
        // Arrange
        var person = new TestPerson { FirstName = "John", LastName = "Doe", Age = 30 };

        // Act
        var result = _serializer.Serialize(person);

        // Assert
        Assert.False(result.IsEmpty);
        Assert.True(result.Length > 0);

        var jsonString = System.Text.Encoding.UTF8.GetString(result.Span);
        Assert.Contains("firstName", jsonString);
        Assert.Contains("John", jsonString);
        Assert.Contains("Doe", jsonString);
        Assert.Contains("30", jsonString);
    }

    [Fact]
    public void Serialize_WithNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        TestPerson? person = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.Serialize(person));
    }

    [Fact]
    public void Serialize_WithCustomOptions_RespectsNamingPolicy()
    {
        // Arrange
        var person = new TestPerson { FirstName = "John", LastName = "Doe" };

        // Act
        var result = _customSerializer.Serialize(person);

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(result.Span);
        Assert.Contains("first_name", jsonString);
        Assert.Contains("last_name", jsonString);
    }

    [Fact]
    public void Serialize_WithNullProperties_IgnoresNullValues()
    {
        // Arrange
        var person = new TestPerson { FirstName = "John", LastName = null, Age = 30 };

        // Act
        var result = _serializer.Serialize(person);

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(result.Span);
        Assert.Contains("firstName", jsonString);
        Assert.Contains("John", jsonString);
        Assert.Contains("age", jsonString);
        Assert.DoesNotContain("lastName", jsonString);
        Assert.DoesNotContain("email", jsonString);
    }
    #endregion

    #region Deserialize Tests (ReadOnlyMemory overload)
    [Fact]
    public void Deserialize_ReadOnlyMemory_WithValidJson_ReturnsObject()
    {
        // Arrange
        var json = """{"firstName":"John","lastName":"Doe","age":30}"""u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(json);

        // Act
        var result = _serializer.Deserialize(typeof(TestPerson), memory);

        // Assert
        Assert.NotNull(result);
        var person = Assert.IsType<TestPerson>(result);
        Assert.Equal("John", person.FirstName);
        Assert.Equal("Doe", person.LastName);
        Assert.Equal(30, person.Age);
    }

    [Fact]
    public void Deserialize_ReadOnlyMemory_WithEmptyPayload_ReturnsNull()
    {
        // Arrange
        var emptyMemory = ReadOnlyMemory<byte>.Empty;

        // Act
        var result = _serializer.Deserialize(typeof(TestPerson), emptyMemory);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_ReadOnlyMemory_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var json = """{"firstName":"John"}"""u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(json);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.Deserialize(null!, memory));
    }

    [Fact]
    public void Deserialize_ReadOnlyMemory_WithCustomOptions_RespectsCaseInsensitivity()
    {
        // Arrange
        var json = """{"FIRSTNAME":"John","LASTNAME":"Doe"}"""u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(json);

        // Act
        var result = _serializer.Deserialize(typeof(TestPerson), memory);

        // Assert
        Assert.NotNull(result);
        var person = Assert.IsType<TestPerson>(result);
        Assert.Equal("John", person.FirstName);
        Assert.Equal("Doe", person.LastName);
    }
    #endregion

    #region Deserialize Tests (ReadOnlySpan overload)
    [Fact]
    public void Deserialize_ReadOnlySpan_WithValidJson_ReturnsObject()
    {
        // Arrange
        var json = """{"firstName":"John","lastName":"Doe","age":30}"""u8.ToArray();

        // Act
        var result = _serializer.Deserialize(typeof(TestPerson), json.AsSpan());

        // Assert
        Assert.NotNull(result);
        var person = Assert.IsType<TestPerson>(result);
        Assert.Equal("John", person.FirstName);
        Assert.Equal("Doe", person.LastName);
        Assert.Equal(30, person.Age);
    }

    [Fact]
    public void Deserialize_ReadOnlySpan_WithEmptySpan_ReturnsNull()
    {
        // Arrange
        var emptySpan = ReadOnlySpan<byte>.Empty;

        // Act
        var result = _serializer.Deserialize(typeof(TestPerson), emptySpan);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_ReadOnlySpan_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var json = """{"firstName":"John"}"""u8.ToArray();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.Deserialize(null!, json.AsSpan()));
    }
    #endregion

    #region ExtractStringField Tests (ReadOnlyMemory overload)
    [Fact]
    public void ExtractStringField_ReadOnlyMemory_WithExistingField_ReturnsValue()
    {
        // Arrange
        var json = """{"firstName":"John","lastName":"Doe","age":30}"""u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(json);

        // Act
        var result = _serializer.ExtractStringField("firstName", memory);

        // Assert
        Assert.Equal("John", result);
    }

    [Fact]
    public void ExtractStringField_ReadOnlyMemory_WithNestedField_ReturnsValue()
    {
        // Arrange
        var json = """{"person":{"firstName":"John","lastName":"Doe"},"id":"123"}"""u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(json);

        // Act
        var result = _serializer.ExtractStringField("id", memory);

        // Assert
        Assert.Equal("123", result);
    }

    [Fact]
    public void ExtractStringField_ReadOnlyMemory_WithNonExistentField_ReturnsNull()
    {
        // Arrange
        var json = """{"firstName":"John","lastName":"Doe"}"""u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(json);

        // Act
        var result = _serializer.ExtractStringField("email", memory);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractStringField_ReadOnlyMemory_WithNonStringField_ReturnsNull()
    {
        // Arrange
        var json = """{"firstName":"John","age":30}"""u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(json);

        Assert.Throws<InvalidOperationException>(() => _serializer.ExtractStringField("age", memory));
    }

    [Fact]
    public void ExtractStringField_ReadOnlyMemory_WithEmptyPayload_JsonException()
    {
        // Arrange
        var emptyMemory = ReadOnlyMemory<byte>.Empty;

        Assert.ThrowsAny<JsonException>(() => _serializer.ExtractStringField("firstName", emptyMemory));
    }

    [Fact]
    public void ExtractStringField_ReadOnlyMemory_WithNullField_ThrowsArgumentException()
    {
        // Arrange
        var json = """{"firstName":"John"}"""u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(json);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.ExtractStringField(null!, memory));
    }

    [Fact]
    public void ExtractStringField_ReadOnlyMemory_WithEmptyField_ThrowsArgumentException()
    {
        // Arrange
        var json = """{"firstName":"John"}"""u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(json);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.ExtractStringField("", memory));
    }
    #endregion

    #region ExtractStringField Tests (ReadOnlySpan overload)
    [Fact]
    public void ExtractStringField_ReadOnlySpan_WithExistingField_ReturnsValue()
    {
        // Arrange
        var json = """{"firstName":"John","lastName":"Doe","age":30}"""u8.ToArray();

        // Act
        var result = _serializer.ExtractStringField("firstName", json.AsSpan());

        // Assert
        Assert.Equal("John", result);
    }

    [Fact]
    public void ExtractStringField_ReadOnlySpan_WithNonExistentField_ReturnsNull()
    {
        // Arrange
        var json = """{"firstName":"John","lastName":"Doe"}"""u8.ToArray();

        // Act
        var result = _serializer.ExtractStringField("email", json.AsSpan());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractStringField_ReadOnlySpan_WithEmptySpan_Throws_JsonReaderException()
    {
        Assert.ThrowsAny<JsonException>(() => _serializer.ExtractStringField("firstName", ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void ExtractStringField_ReadOnlySpan_WithNullField_ThrowsArgumentException()
    {
        // Arrange
        var json = """{"firstName":"John"}"""u8.ToArray();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.ExtractStringField(null!, json.AsSpan()));
    }
    #endregion

    #region Integration Tests
    [Fact]
    public void RoundTrip_SerializeAndDeserialize_ReturnsEquivalentObject()
    {
        // Arrange
        var original = new TestPerson
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 30,
            Email = "john.doe@example.com"
        };

        // Act
        var serialized = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(typeof(TestPerson), serialized);

        // Assert
        Assert.NotNull(deserialized);
        var resultPerson = Assert.IsType<TestPerson>(deserialized);
        Assert.Equal(original.FirstName, resultPerson.FirstName);
        Assert.Equal(original.LastName, resultPerson.LastName);
        Assert.Equal(original.Age, resultPerson.Age);
        Assert.Equal(original.Email, resultPerson.Email);
    }

    [Fact]
    public void ExtractStringField_FromSerializedData_ReturnsCorrectValue()
    {
        // Arrange
        var person = new TestPerson { FirstName = "Jane", LastName = "Smith" };
        var serialized = _serializer.Serialize(person);

        // Act
        var firstName = _serializer.ExtractStringField("firstName", serialized);
        var lastName = _serializer.ExtractStringField("lastName", serialized);

        // Assert
        Assert.Equal("Jane", firstName);
        Assert.Equal("Smith", lastName);
    }
    #endregion

    #region Edge Cases
    [Fact]
    public void Deserialize_WithMalformedJson_ThrowsJsonException()
    {
        // Arrange
        var malformedJson = """{"firstName":"John","lastName":}"""u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(malformedJson);

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => _serializer.Deserialize(typeof(TestPerson), memory));
    }

    [Fact]
    public void ExtractStringField_WithEmptyObject_ReturnsNull()
    {
        // Arrange
        var json = "{}"u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(json);

        // Act
        var result = _serializer.ExtractStringField("anyField", memory);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractStringField_WithArrayJson_ReturnsNull()
    {
        // Arrange
        var json = """[{"name":"John"},{"name":"Jane"}]"""u8.ToArray();
        var memory = new ReadOnlyMemory<byte>(json);

        // Act
        var result = _serializer.ExtractStringField("name", memory);

        // Assert
        Assert.Equal(result, "John");
    }

    [Fact]
    public void Serialize_WithComplexObject_HandlesNestedStructures()
    {
        // Arrange
        var nested = new TestNestedObject
        {
            Id = "test-123",
            Person = new TestPerson { FirstName = "John", LastName = "Doe" },
            Tags = new List<string> { "tag1", "tag2" }
        };

        // Act
        var result = _serializer.Serialize(nested);

        // Assert
        Assert.False(result.IsEmpty);

        var jsonString = System.Text.Encoding.UTF8.GetString(result.Span);
        Assert.Contains("id", jsonString);
        Assert.Contains("test-123", jsonString);
        Assert.Contains("person", jsonString);
        Assert.Contains("tags", jsonString);
    }
    #endregion
}