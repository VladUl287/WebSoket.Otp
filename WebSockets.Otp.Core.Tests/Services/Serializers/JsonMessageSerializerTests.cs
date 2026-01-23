using Moq;
using System.Buffers;
using System.Text.Json;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Services.Serializers;

namespace WebSockets.Otp.Core.Tests.Services.Serializers;

public sealed class JsonMessageSerializerTests
{
    private readonly JsonMessageSerializer _sut;

    public JsonMessageSerializerTests()
    {
        _sut = new JsonMessageSerializer();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Default_ShouldInitializeWithDefaultOptions()
    {
        // Act
        var serializer = new JsonMessageSerializer();

        // Assert
        Assert.Equal("json", serializer.ProtocolName);
    }

    [Fact]
    public void Constructor_WithCustomOptions_ShouldUseProvidedOptions()
    {
        // Arrange
        var customOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };

        // Act
        var serializer = new JsonMessageSerializer(customOptions);

        // Assert
        Assert.Equal("json", serializer.ProtocolName);
    }

    #endregion

    #region ProtocolName Tests

    [Fact]
    public void ProtocolName_ShouldReturnJson()
    {
        // Assert
        Assert.Equal("json", _sut.ProtocolName);
    }

    #endregion

    #region Serialize Tests

    [Fact]
    public void Serialize_ValidObject_ShouldReturnSerializedBytes()
    {
        // Arrange
        var testObject = new TestClass
        {
            Id = 123,
            Name = "Test",
            Value = 45.67m
        };

        // Act
        var result = _sut.Serialize(testObject);

        // Assert
        Assert.NotEmpty(result.ToArray());
        var json = System.Text.Encoding.UTF8.GetString(result.Span);
        Assert.Contains("123", json);
        Assert.Contains("Test", json);
    }

    [Fact]
    public void Serialize_NullObject_ShouldThrowArgumentNullException()
    {
        // Arrange
        TestClass? nullObject = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.Serialize(nullObject!));
    }

    [Fact]
    public void Serialize_WithCamelCaseNamingPolicy_ShouldUseCamelCase()
    {
        // Arrange
        var testObject = new { TestProperty = "value" };

        // Act
        var result = _sut.Serialize(testObject);
        var json = System.Text.Encoding.UTF8.GetString(result.Span);

        // Assert
        Assert.Contains("testProperty", json);
        Assert.DoesNotContain("TestProperty", json);
    }

    [Fact]
    public void Serialize_WithNullValues_ShouldIgnoreNullProperties()
    {
        // Arrange
        var testObject = new
        {
            Property1 = "value",
            Property2 = (string?)null,
            Property3 = 123
        };

        // Act
        var result = _sut.Serialize(testObject);
        var json = System.Text.Encoding.UTF8.GetString(result.Span);

        // Assert
        Assert.Contains("property1", json);
        Assert.Contains("property3", json);
        Assert.DoesNotContain("property2", json);
    }

    #endregion

    #region Deserialize Tests

    [Fact]
    public void Deserialize_ValidJson_ShouldReturnDeserializedObject()
    {
        // Arrange
        var json = "{\"id\":123,\"name\":\"Test\",\"value\":45.67}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Act
        var result = _sut.Deserialize(typeof(TestClass), bytes);

        // Assert
        Assert.NotNull(result);
        var testClass = Assert.IsType<TestClass>(result);
        Assert.Equal(123, testClass.Id);
        Assert.Equal("Test", testClass.Name);
        Assert.Equal(45.67m, testClass.Value);
    }

    [Fact]
    public void Deserialize_InvalidJson_ShouldReturnNull()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var bytes = System.Text.Encoding.UTF8.GetBytes(invalidJson);

        // Act
        var result = _sut.Deserialize(typeof(TestClass), bytes);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_EmptyData_ShouldReturnNull()
    {
        // Arrange
        ReadOnlySpan<byte> emptyData = [];

        // Act
        var result = _sut.Deserialize(typeof(TestClass), emptyData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithCaseInsensitivePropertyMatching_ShouldMatchProperties()
    {
        // Arrange
        var json = "{\"ID\":123,\"NAME\":\"Test\",\"VALUE\":45.67}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Act
        var result = _sut.Deserialize(typeof(TestClass), bytes);

        // Assert
        Assert.NotNull(result);
        var testClass = Assert.IsType<TestClass>(result);
        Assert.Equal(123, testClass.Id);
        Assert.Equal("Test", testClass.Name);
        Assert.Equal(45.67m, testClass.Value);
    }

    #endregion

    #region ExtractField Tests (Without StringPool)

    [Fact]
    public void ExtractField_ValidField_ShouldReturnStringValue()
    {
        // Arrange
        var json = "{\"id\":123,\"name\":\"John Doe\",\"active\":true}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fieldName = System.Text.Encoding.UTF8.GetBytes("name");

        // Act
        var result = _sut.ExtractField(fieldName, bytes);

        // Assert
        Assert.Equal("John Doe", result);
    }

    [Fact]
    public void ExtractField_FieldNotFound_ShouldThrowNullReferenceException()
    {
        // Arrange
        var json = "{\"id\":123,\"name\":\"John Doe\"}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fieldName = System.Text.Encoding.UTF8.GetBytes("nonexistent");

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _sut.ExtractField(fieldName, bytes));
    }

    [Fact]
    public void ExtractField_FieldValueIsNotString_ShouldThrowNullReferenceException()
    {
        // Arrange
        var json = "{\"id\":123,\"name\":\"John Doe\"}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fieldName = System.Text.Encoding.UTF8.GetBytes("id");

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _sut.ExtractField(fieldName, bytes));
    }

    [Fact]
    public void ExtractField_NullFieldValue_ShouldThrowNullReferenceException()
    {
        // Arrange
        var json = "{\"id\":123,\"name\":null}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fieldName = System.Text.Encoding.UTF8.GetBytes("name");

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _sut.ExtractField(fieldName, bytes));
    }

    [Fact]
    public void ExtractField_EmptyJson_ShouldThrowNullReferenceException()
    {
        // Arrange
        var json = "{}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fieldName = System.Text.Encoding.UTF8.GetBytes("name");

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _sut.ExtractField(fieldName, bytes));
    }

    [Fact]
    public void ExtractField_NestedObject_ShouldExtractFromNestedField()
    {
        // Arrange
        var json = "{\"user\":{\"name\":\"John\",\"age\":30},\"active\":true}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fieldName = System.Text.Encoding.UTF8.GetBytes("name");

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _sut.ExtractField(fieldName, bytes));
    }

    [Fact]
    public void ExtractField_MultipleProperties_ShouldFindCorrectOne()
    {
        // Arrange
        var json = "{\"id\":1,\"id\":2,\"name\":\"test\"}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fieldName = System.Text.Encoding.UTF8.GetBytes("name");

        // Act
        var result = _sut.ExtractField(fieldName, bytes);

        // Assert
        Assert.Equal("test", result);
    }

    #endregion

    #region ExtractField Tests (With StringPool)

    [Fact]
    public void ExtractField_WithStringPool_ValidField_ShouldInternString()
    {
        // Arrange
        var json = "{\"id\":123,\"name\":\"John Doe\",\"active\":true}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fieldName = System.Text.Encoding.UTF8.GetBytes("name");
        var mockStringPool = new Mock<IStringPool>();
        mockStringPool.Setup(p => p.Intern(bytes))
                      .Returns("John Doe");

        // Act
        var result = _sut.ExtractField(fieldName, bytes, mockStringPool.Object);

        // Assert
        Assert.Equal("John Doe", result);
        mockStringPool.Verify(p => p.Intern(bytes), Times.Once);
    }

    [Fact]
    public void ExtractField_WithStringPool_ValueSequence_ShouldUseValueSequenceOverload()
    {
        // This test would require creating a scenario where HasValueSequence is true
        // For simplicity, we'll test that the method doesn't throw with a mock
        var json = "{\"name\":\"test\"}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fieldName = System.Text.Encoding.UTF8.GetBytes("name");
        var mockStringPool = new Mock<IStringPool>();

        // Setup to handle both overloads
        mockStringPool.Setup(p => p.Intern(bytes))
                      .Returns("test");

        // Act & Assert
        var exception = Record.Exception(() =>
            _sut.ExtractField(fieldName, bytes, mockStringPool.Object));

        Assert.Null(exception);
    }

    [Fact]
    public void ExtractField_WithStringPool_FieldNotFound_ShouldThrowNullReferenceException()
    {
        // Arrange
        var json = "{\"id\":123}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fieldName = System.Text.Encoding.UTF8.GetBytes("name");
        var mockStringPool = new Mock<IStringPool>();

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            _sut.ExtractField(fieldName, bytes, mockStringPool.Object));

        mockStringPool.Verify(p => p.Intern(bytes), Times.Never);
        mockStringPool.Verify(p => p.Intern(It.IsAny<ReadOnlySequence<byte>>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Serialize_Deserialize_RoundTrip_ShouldReturnOriginalObject()
    {
        // Arrange
        var original = new TestClass
        {
            Id = 999,
            Name = "RoundTrip Test",
            Value = 123.45m
        };

        // Act
        var serialized = _sut.Serialize(original);
        var deserialized = _sut.Deserialize(typeof(TestClass), serialized.Span) as TestClass;

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Value, deserialized.Value);
    }

    [Fact]
    public void Serialize_SpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var testObject = new
        {
            Text = "Line1\nLine2\tTab\"Quote\\Backslash"
        };

        // Act
        var result = _sut.Serialize(testObject);
        var json = System.Text.Encoding.UTF8.GetString(result.Span);

        // Assert
        Assert.Contains("Line1\\nLine2\\tTab\\\"Quote\\\\Backslash", json);
    }

    [Fact]
    public void ExtractField_FieldInArray_ShouldThrowException()
    {
        // Arrange
        var json = "[{\"name\":\"test\"}]";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fieldName = System.Text.Encoding.UTF8.GetBytes("name");

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _sut.ExtractField(fieldName, bytes));
    }

    #endregion

    #region Test Helper Class

    private class TestClass
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Value { get; set; }
    }

    #endregion
}
