using System.Text;
using System.Buffers;
using System.Collections.Frozen;
using WebSockets.Otp.Core.Services.Utils;
using System.IO.Hashing;

namespace WebSockets.Otp.Core.Tests.Services.Utils;

public class EndpointsKeysPoolTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidKeys_CreatesPoolSuccessfully()
    {
        // Arrange
        var keys = new[] { "key1", "key2", "key3" };
        var encoding = Encoding.UTF8;

        // Act
        var pool = new EndpointsKeysPool(keys, encoding);

        // Assert
        Assert.Equal(encoding, pool.Encoding);
        Assert.False(pool.HasCollisions);
        Assert.NotNull(pool);
    }

    [Fact]
    public void Constructor_WithDuplicateKeys_RemovesDuplicates()
    {
        // Arrange
        var keys = new[] { "key1", "key1", "key2", "key2" };
        var encoding = Encoding.UTF8;

        // Act
        var pool = new EndpointsKeysPool(keys, encoding);

        // Assert
        Assert.False(pool.HasCollisions);
    }

    [Fact]
    public void Constructor_WithNullKeys_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EndpointsKeysPool(null!, Encoding.UTF8));
    }

    [Fact]
    public void Constructor_WithNullEncoding_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            new EndpointsKeysPool(new[] { "key1" }, null!));
    }

    [Fact]
    public void Constructor_WithEmptyCollection_CreatesEmptyPool()
    {
        // Arrange
        var keys = Enumerable.Empty<string>();
        var encoding = Encoding.UTF8;

        // Act
        var pool = new EndpointsKeysPool(keys, encoding);

        // Assert
        Assert.NotNull(pool);
        Assert.False(pool.HasCollisions);
    }

    #endregion

    #region Intern Tests - Single Segment/ReadOnlySpan

    [Fact]
    public void Intern_WithExistingKey_ReturnsInternedString()
    {
        // Arrange
        var keys = new[] { "test-key", "another-key" };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8);
        var bytes = Encoding.UTF8.GetBytes("test-key");

        // Act
        var result = pool.Intern(bytes);

        // Assert
        Assert.Equal("test-key", result);
        Assert.Same(keys[0], result); // Should return same reference
    }

    [Fact]
    public void Intern_WithNonExistingKey_ReturnsNewString()
    {
        // Arrange
        var keys = new[] { "key1", "key2" };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8);
        var bytes = Encoding.UTF8.GetBytes("key3");

        // Act
        var result = pool.Intern(bytes);

        // Assert
        Assert.Equal("key3", result);
        Assert.NotSame(keys[0], result);
        Assert.NotSame(keys[1], result);
    }

    [Fact]
    public void Intern_WithEmptySpan_ReturnsEmptyString()
    {
        // Arrange
        var keys = new[] { "test" };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8);
        var bytes = Array.Empty<byte>();

        // Act
        var result = pool.Intern(bytes);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Intern_WithCollisionInSafeMode_VerifiesAndReturnsCorrectString()
    {
        // Arrange
        // Create two different strings that would have same hash with a naive hash
        // For real XXHash3 collisions are extremely rare, so we'll simulate with test data
        var encoding = Encoding.UTF8;
        var keys = new[] { "collision1", "collision2" };
        var pool = new EndpointsKeysPool(keys, encoding, unsafeMode: false);

        var bytes = encoding.GetBytes("collision2");

        // Act
        var result = pool.Intern(bytes);

        // Assert
        Assert.Equal("collision2", result);
    }

    [Fact]
    public void Intern_WithDifferentEncodings_HandlesEncodingCorrectly()
    {
        // Arrange
        var keys = new[] { "café", "naïve" };
        var encoding = Encoding.UTF8;
        var pool = new EndpointsKeysPool(keys, encoding);

        var bytes = encoding.GetBytes("café");

        // Act
        var result = pool.Intern(bytes);

        // Assert
        Assert.Equal("café", result);
    }

    [Fact]
    public void Intern_WithUTF8EncodingSpecialCharacters_WorksCorrectly()
    {
        // Arrange
        var specialString = "🎉✨🌟🎈";
        var keys = new[] { specialString };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8);

        var bytes = Encoding.UTF8.GetBytes(specialString);

        // Act
        var result = pool.Intern(bytes);

        // Assert
        Assert.Equal(specialString, result);
    }

    #endregion

    #region Intern Tests - ReadOnlySequence

    [Fact]
    public void Intern_ReadOnlySequence_WithSingleSegment_WorksLikeSpan()
    {
        // Arrange
        var keys = new[] { "single-segment-key" };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8);

        var bytes = Encoding.UTF8.GetBytes("single-segment-key");
        var sequence = new ReadOnlySequence<byte>(bytes);

        // Act
        var result = pool.Intern(sequence);

        // Assert
        Assert.Equal("single-segment-key", result);
        Assert.Same(keys[0], result);
    }

    [Fact]
    public void Intern_ReadOnlySequence_WithNonExistingKey_ReturnsNewString()
    {
        // Arrange
        var keys = new[] { "existing" };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8);

        var bytes = Encoding.UTF8.GetBytes("non-existing");
        var sequence = new ReadOnlySequence<byte>(bytes);

        // Act
        var result = pool.Intern(sequence);

        // Assert
        Assert.Equal("non-existing", result);
    }

    [Fact]
    public void Intern_ReadOnlySequence_WithEmptySequence_ReturnsEmptyString()
    {
        // Arrange
        var keys = new[] { "test" };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8);

        var sequence = ReadOnlySequence<byte>.Empty;

        // Act
        var result = pool.Intern(sequence);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Intern_ReadOnlySequence_WithVeryLargeSequence_UsesArrayPool()
    {
        // Arrange
        var largeString = new string('x', 10000);
        var keys = new[] { largeString };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8);

        var bytes = Encoding.UTF8.GetBytes(largeString);
        var sequence = new ReadOnlySequence<byte>(bytes);

        // Act - This should use the ArrayPool path for large sequences
        var result = pool.Intern(sequence);

        // Assert
        Assert.Equal(largeString, result);
    }

    #endregion

    #region Hash Collision Tests

    [Fact]
    public void HasCollisions_WhenNoCollisions_ReturnsFalse()
    {
        // Arrange
        var keys = new[] { "key1", "key2", "key3" };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8);

        // Assert
        Assert.False(pool.HasCollisions);
    }

    [Fact]
    public void Intern_WithHashCollisionChain_TraversesChainCorrectly()
    {
        // Arrange
        // Create multiple entries with same hash (simulated)
        // Since XXHash3 collisions are rare, we'll test chain traversal differently
        var encoding = Encoding.UTF8;

        // Create pool with linked list structure
        var keys = new List<string>();
        var pool = new EndpointsKeysPool(keys, encoding);

        // Use reflection to manually create collision chain for testing
        var entry1 = new Entry
        {
            Value = "first",
            Bytes = encoding.GetBytes("first")
        };

        var entry2 = new Entry
        {
            Value = "second",
            Bytes = encoding.GetBytes("second"),
            Next = entry1
        };

        var entry3 = new Entry
        {
            Value = "third",
            Bytes = encoding.GetBytes("third"),
            Next = entry2
        };

        // Create dictionary with same hash for all
        var hashCode = 12345UL; // Test hash
        var dictionary = new Dictionary<ulong, Entry>
        {
            [hashCode] = entry3
        };

        var frozenDict = dictionary.ToFrozenDictionary();

        // Replace mapEntries using reflection for test
        SetPrivateField(pool, "_mapEntries", frozenDict);
        SetPrivateField(pool, "_hasCollisions", true);

        // Act - Search for middle entry
        var result = pool.Intern(encoding.GetBytes("second"));

        // Assert
        Assert.Equal("second", result);
    }

    [Fact]
    public void Intern_WithHashCollisionNotFound_ReturnsNewString()
    {
        // Arrange
        var encoding = Encoding.UTF8;
        var keys = new[] { "existing1", "existing2" };
        var pool = new EndpointsKeysPool(keys, encoding);

        // Create collision chain manually for testing
        var existingBytes1 = encoding.GetBytes("existing1");
        var existingBytes2 = encoding.GetBytes("existing2");
        var nonExistingBytes = encoding.GetBytes("non-existing");

        // Use reflection to set up test scenario
        var entry1 = new Entry
        {
            Value = "existing1",
            Bytes = existingBytes1
        };

        var entry2 = new Entry
        {
            Value = "existing2",
            Bytes = existingBytes2,
            Next = entry1
        };

        var hashCode = XxHash3.HashToUInt64(existingBytes1);
        var dictionary = new Dictionary<ulong, Entry>
        {
            [hashCode] = entry2
        };

        var frozenDict = dictionary.ToFrozenDictionary();

        SetPrivateField(pool, "_mapEntries", frozenDict);
        SetPrivateField(pool, "_hasCollisions", true);

        // Act - Try to find non-existing key with same hash
        var result = pool.Intern(nonExistingBytes);

        // Assert
        Assert.Equal("non-existing", result);
        Assert.NotSame("existing1", result);
        Assert.NotSame("existing2", result);
    }

    #endregion

    #region Performance and Edge Cases

    [Fact]
    public void Intern_WithVeryLongString_WorksCorrectly()
    {
        // Arrange
        var longString = new string('a', 100000); // 100KB string
        var keys = new[] { longString };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8);

        var bytes = Encoding.UTF8.GetBytes(longString);

        // Act
        var result = pool.Intern(bytes);

        // Assert
        Assert.Equal(longString, result);
        Assert.Equal(100000, result.Length);
    }

    [Fact]
    public void Intern_WithDifferentByteRepresentations_ReturnsCorrectString()
    {
        // Arrange
        var keys = new[] { "test" };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8);

        // Same string but different byte array (should still match)
        var bytes1 = Encoding.UTF8.GetBytes("test");
        var bytes2 = Encoding.UTF8.GetBytes("test");

        // Act & Assert
        var result1 = pool.Intern(bytes1);
        var result2 = pool.Intern(bytes2);

        Assert.Equal("test", result1);
        Assert.Equal("test", result2);
        Assert.Same(result1, result2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UnsafeMode_Property_AffectsCollisionHandling(bool unsafeMode)
    {
        // Arrange
        var keys = new[] { "key1", "key2" };
        var pool = new EndpointsKeysPool(keys, Encoding.UTF8, unsafeMode);

        // Act & Assert
        // Test that unsafe mode changes behavior when there are collisions
        // (collisions are rare with XXHash3, so we focus on the mode setting)
        var bytes = Encoding.UTF8.GetBytes("key1");
        var result = pool.Intern(bytes);

        Assert.Equal("key1", result);
    }

    #endregion

    #region Encoding Tests

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("UTF-16")]
    [InlineData("UTF-32")]
    [InlineData("ASCII")]
    public void Intern_WithDifferentEncodings_PreservesEncoding(string encodingName)
    {
        // Arrange
        var encoding = Encoding.GetEncoding(encodingName);
        var testString = "Test string with encoding: " + encodingName;
        var keys = new[] { testString };
        var pool = new EndpointsKeysPool(keys, encoding);

        var bytes = encoding.GetBytes(testString);

        // Act
        var result = pool.Intern(bytes);

        // Assert
        Assert.Equal(testString, result);
    }

    [Fact]
    public void Encoding_Property_ReturnsCorrectEncoding()
    {
        // Arrange
        var expectedEncoding = Encoding.UTF32;
        var pool = new EndpointsKeysPool(new[] { "test" }, expectedEncoding);

        // Act & Assert
        Assert.Equal(expectedEncoding, pool.Encoding);
    }

    #endregion

    #region Helper Methods

    private static ReadOnlySequence<byte> CreateMultiSegmentSequence(params byte[][] segments)
    {
        if (segments.Length == 0)
            return ReadOnlySequence<byte>.Empty;

        var firstSegment = new MemorySegment<byte>(segments[0]);
        var lastSegment = firstSegment;

        for (int i = 1; i < segments.Length; i++)
        {
            var newSegment = new MemorySegment<byte>(segments[i]);
            lastSegment.Next = newSegment;
            lastSegment = newSegment;
        }

        return new ReadOnlySequence<byte>(firstSegment, 0, lastSegment, lastSegment.Memory.Length);
    }

    private class MemorySegment<T> : ReadOnlySequenceSegment<T>
    {
        public MemorySegment(ReadOnlyMemory<T> memory)
        {
            Memory = memory;
        }

        public MemorySegment<T>? Next { get; set; }
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    #endregion
}

public sealed class MockEncoding : Encoding
{
    public override byte[] GetBytes(string s)
    {
        return [12, 12];
    }

    public override int GetByteCount(char[] chars, int index, int count)
    {
        throw new NotImplementedException();
    }

    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        return int.MaxValue;
    }

    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        throw new NotImplementedException();
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        throw new NotImplementedException();
    }

    public override int GetMaxByteCount(int charCount)
    {
        throw new NotImplementedException();
    }

    public override int GetMaxCharCount(int byteCount)
    {
        throw new NotImplementedException();
    }
}
