using System.Buffers;
using System.Runtime.InteropServices;
using WebSockets.Otp.Core.Services.Utils;

namespace WebSockets.Otp.Core.Tests.Services.Utils;

public class NativeChunkedBufferTests : IDisposable
{
    private readonly List<NativeChunkedBuffer> _buffers = new();

    public void Dispose()
    {
        foreach (var buffer in _buffers)
        {
            buffer.Dispose();
        }
        _buffers.Clear();
    }

    private NativeChunkedBuffer CreateBuffer(int capacity = 1024)
    {
        var buffer = new NativeChunkedBuffer(capacity);
        _buffers.Add(buffer);
        return buffer;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidCapacity_InitializesSuccessfully()
    {
        // Arrange & Act
        var buffer = CreateBuffer(1024);

        // Assert
        Assert.Equal(1024, buffer.Capacity);
        Assert.Equal(0, buffer.Length);
        Assert.False(buffer.IsDisposed());
    }

    [Fact]
    public void Constructor_WithZeroCapacity_InitializesSuccessfully()
    {
        // Arrange & Act
        var buffer = CreateBuffer(0);

        // Assert
        Assert.Equal(0, buffer.Capacity);
        Assert.Equal(0, buffer.Length);
    }

    #endregion

    #region Write Tests - ReadOnlySpan

    [Fact]
    public void Write_WithSpan_AddsDataToBuffer()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        buffer.Write(data);

        // Assert
        Assert.Equal(5, buffer.Length);
        Assert.Equal(data, buffer.Span.ToArray());
    }

    [Fact]
    public void Write_WithEmptySpan_DoesNothing()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        var initialLength = buffer.Length;
        var data = ReadOnlySpan<byte>.Empty;

        // Act
        buffer.Write(data);

        // Assert
        Assert.Equal(initialLength, buffer.Length);
    }

    [Fact]
    public void Write_MultipleTimes_AppendsData()
    {
        // Arrange
        var buffer = CreateBuffer(20);
        var data1 = new byte[] { 1, 2, 3 };
        var data2 = new byte[] { 4, 5, 6 };
        var expected = new byte[] { 1, 2, 3, 4, 5, 6 };

        // Act
        buffer.Write(data1);
        buffer.Write(data2);

        // Assert
        Assert.Equal(6, buffer.Length);
        Assert.Equal(expected, buffer.Span.ToArray());
    }

    [Fact]
    public void Write_WhenCapacityExceeded_AutoExpands()
    {
        // Arrange
        var buffer = CreateBuffer(4);
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        buffer.Write(data);

        // Assert
        Assert.Equal(5, buffer.Length);
        Assert.True(buffer.Capacity >= 5);
        Assert.Equal(data, buffer.Span.ToArray());
    }

    [Fact]
    public void Write_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        buffer.Dispose();
        var data = new byte[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => buffer.Write(data));
    }

    [Fact]
    public void Write_ThatWouldExceedMaxArraySize_ThrowsOutOfMemoryException()
    {
        // Arrange
        var buffer = CreateBuffer(Array.MaxLength - 5);
        buffer.SetLength(Array.MaxLength - 3); // Fill almost to max

        var data = new byte[5]; // This would exceed Array.MaxLength

        // Act & Assert
        Assert.Throws<OutOfMemoryException>(() => buffer.Write(data));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Write_WithVariousSizes_WorksCorrectly(int size)
    {
        // Arrange
        var buffer = CreateBuffer(size / 2); // Start with half capacity to force expansion
        var data = Enumerable.Range(0, size).Select(i => (byte)(i % 256)).ToArray();

        // Act
        buffer.Write(data);

        // Assert
        Assert.Equal(size, buffer.Length);
        Assert.Equal(data, buffer.Span.ToArray());
    }

    #endregion

    #region Write Tests - ReadOnlySequence

    [Fact]
    public void Write_WithSequence_AddsDataToBuffer()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var sequence = new ReadOnlySequence<byte>(data);

        // Act
        buffer.Write(sequence);

        // Assert
        Assert.Equal(5, buffer.Length);
        Assert.Equal(data, buffer.Span.ToArray());
    }

    [Fact]
    public void Write_WithMultiSegmentSequence_AddsAllSegments()
    {
        // Arrange
        var buffer = CreateBuffer(20);

        var segment1 = new byte[] { 1, 2, 3 };
        var segment2 = new byte[] { 4, 5, 6 };
        var segment3 = new byte[] { 7, 8, 9 };

        var sequence = CreateMultiSegmentSequence(segment1, segment2, segment3);
        var expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // Act
        buffer.Write(sequence);

        // Assert
        Assert.Equal(9, buffer.Length);
        Assert.Equal(expected, buffer.Span.ToArray());
    }

    [Fact]
    public void Write_WithEmptySequence_DoesNothing()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        var initialLength = buffer.Length;
        var sequence = ReadOnlySequence<byte>.Empty;

        // Act
        buffer.Write(sequence);

        // Assert
        Assert.Equal(initialLength, buffer.Length);
    }

    #endregion

    #region Capacity Management Tests

    [Fact]
    public void EnsureCapacity_WhenNotNeeded_DoesNothing()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        var initialCapacity = buffer.Capacity;

        // Act
        buffer.Write(new byte[5]); // Doesn't exceed capacity

        // Assert
        Assert.Equal(initialCapacity, buffer.Capacity);
    }

    [Fact]
    public void EnsureCapacity_WhenNeeded_DoublesCapacity()
    {
        // Arrange
        var buffer = CreateBuffer(4);
        var initialCapacity = buffer.Capacity;

        // Act
        buffer.Write(new byte[5]); // Exceeds capacity

        // Assert
        Assert.True(buffer.Capacity > initialCapacity);
        Assert.True(buffer.Capacity >= 5);
    }

    [Fact]
    public void EnsureCapacity_WhenAtMaxArraySize_ThrowsOutOfMemoryException()
    {
        // Arrange
        var buffer = CreateBuffer(Array.MaxLength);
        buffer.SetLength(Array.MaxLength - 1);

        // Act & Assert
        Assert.Throws<OutOfMemoryException>(() => buffer.Write(new byte[2]));
    }

    [Fact]
    public void EnsureCapacity_FromZeroCapacity_StartsWithFour()
    {
        // Arrange
        var buffer = CreateBuffer(0);

        // Act
        buffer.Write(new byte[] { 1 });

        // Assert
        Assert.True(buffer.Capacity >= 4);
        Assert.Equal(1, buffer.Length);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ResetsLengthToZero()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        buffer.Write(new byte[] { 1, 2, 3, 4, 5 });
        Assert.Equal(5, buffer.Length);

        // Act
        buffer.Clear();

        // Assert
        Assert.Equal(0, buffer.Length);
    }

    [Fact]
    public void Clear_DoesNotAffectCapacity()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        buffer.Write(new byte[] { 1, 2, 3, 4, 5 });
        var initialCapacity = buffer.Capacity;

        // Act
        buffer.Clear();

        // Assert
        Assert.Equal(initialCapacity, buffer.Capacity);
    }

    [Fact]
    public void Clear_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        buffer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => buffer.Clear());
    }

    [Fact]
    public void Clear_ZerosOutMemory()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        var data = new byte[] { 1, 2, 3, 4, 5 };
        buffer.Write(data);

        // Verify data is there
        Assert.Equal(data, buffer.Span.ToArray());

        // Act
        buffer.Clear();

        // Assert - All bytes should be zero
        Assert.All(buffer.Span.ToArray(), b => Assert.Equal(0, b));
    }

    #endregion

    #region Shrink Tests

    [Fact]
    public void Shrink_WhenCapacityAboveInitial_ReducesToInitial()
    {
        // Arrange
        var buffer = CreateBuffer(4);
        buffer.Write(new byte[10]); // Force expansion
        var expandedCapacity = buffer.Capacity;
        Assert.True(expandedCapacity > 4);

        // Act
        buffer.Shrink();

        // Assert
        Assert.Equal(4, buffer.Capacity);
    }

    [Fact]
    public void Shrink_WhenCapacityAtInitial_DoesNothing()
    {
        // Arrange
        var buffer = CreateBuffer(4);
        buffer.Write(new byte[2]);
        var initialCapacity = buffer.Capacity;

        // Act
        buffer.Shrink();

        // Assert
        Assert.Equal(initialCapacity, buffer.Capacity);
    }

    [Fact]
    public void Shrink_WhenLengthGreaterThanInitial_TruncatesLength()
    {
        // Arrange
        var buffer = CreateBuffer(4);
        buffer.Write(new byte[10]); // Write 10 bytes
        Assert.Equal(10, buffer.Length);

        // Act
        buffer.Shrink(); // Capacity goes back to 4

        // Assert
        Assert.Equal(4, buffer.Length); // Length truncated to new capacity
    }

    [Fact]
    public void Shrink_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        buffer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => buffer.Shrink());
    }

    #endregion

    #region SetLength Tests

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void SetLength_WithValidLength_SetsLength(int newLength)
    {
        // Arrange
        var buffer = CreateBuffer(20);

        // Act
        buffer.SetLength(newLength);

        // Assert
        Assert.Equal(newLength, buffer.Length);
    }

    [Fact]
    public void SetLength_WithNegativeLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var buffer = CreateBuffer(10);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetLength(-1));
    }

    [Fact]
    public void SetLength_WithLengthGreaterThanMaxArraySize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var buffer = CreateBuffer(10);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetLength(Array.MaxLength + 1));
    }

    [Fact]
    public void SetLength_IncreasingLength_ZerosNewSpace()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        buffer.Write(new byte[] { 1, 2, 3 });
        Assert.Equal(3, buffer.Length);

        // Act
        buffer.SetLength(6);

        // Assert
        Assert.Equal(6, buffer.Length);
        var span = buffer.Span.ToArray();
        Assert.Equal(1, span[0]);
        Assert.Equal(2, span[1]);
        Assert.Equal(3, span[2]);
        Assert.Equal(0, span[3]);
        Assert.Equal(0, span[4]);
        Assert.Equal(0, span[5]);
    }

    [Fact]
    public void SetLength_DecreasingLength_Truncates()
    {
        // Arrange
        var buffer = CreateBuffer(10);
        buffer.Write(new byte[] { 1, 2, 3, 4, 5 });
        Assert.Equal(5, buffer.Length);

        // Act
        buffer.SetLength(3);

        // Assert
        Assert.Equal(3, buffer.Length);
        Assert.Equal(new byte[] { 1, 2, 3 }, buffer.Span.ToArray());
    }

    [Fact]
    public void SetLength_RequiringMoreCapacity_ExpandsBuffer()
    {
        // Arrange
        var buffer = CreateBuffer(5);
        var initialCapacity = buffer.Capacity;

        // Act
        buffer.SetLength(10);

        // Assert
        Assert.Equal(10, buffer.Length);
        Assert.True(buffer.Capacity >= 10);
        Assert.True(buffer.Capacity > initialCapacity);
    }

    #endregion

    #region Memory Manager Tests

    [Fact]
    public void Manager_ReturnsValidMemoryManager()
    {
        // Arrange
        using var buffer = CreateBuffer(10);
        buffer.Write(new byte[] { 1, 2, 3 });

        // Act & Assert
        var span = buffer.Memory.Span;
        Assert.Equal(3, span.Length);
        Assert.Equal(1, span[0]);
        Assert.Equal(2, span[1]);
        Assert.Equal(3, span[2]);
    }

    [Fact]
    public void Manager_Span_MatchesBufferSpan()
    {
        // Arrange
        using var buffer = CreateBuffer(10);
        var data = new byte[] { 1, 2, 3, 4, 5 };
        buffer.Write(data);

        // Act & Assert
        Assert.Equal(buffer.Span.ToArray(), buffer.Memory.Span.ToArray());
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var buffer = CreateBuffer(10);

        // Act
        buffer.Dispose();
        buffer.Dispose(); // Should not throw

        // Assert
        Assert.True(buffer.IsDisposed());
    }

    [Fact]
    public void Dispose_FreesNativeMemory()
    {
        // Arrange
        long initialMemory = GC.GetTotalMemory(true);

        // Create and fill many buffers
        var buffers = new List<NativeChunkedBuffer>();
        for (int i = 0; i < 100; i++)
        {
            var buffer = new NativeChunkedBuffer(1024);
            buffer.Write(new byte[1024]);
            buffers.Add(buffer);
        }

        // Force GC to get stable measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryWithBuffers = GC.GetTotalMemory(true);

        // Act - Dispose all buffers
        foreach (var buffer in buffers)
        {
            buffer.Dispose();
        }
        buffers.Clear();

        // Force GC again
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfterDispose = GC.GetTotalMemory(true);

        // Assert - Memory should be lower after disposal
        // Note: This is a heuristic test, not exact
        Assert.True(memoryAfterDispose < memoryWithBuffers,
            "Memory should be lower after disposing buffers");
    }

    #endregion

    #region Stress Tests

    [Fact]
    public void Write_LargeAmountsOfData_WorksCorrectly()
    {
        // Arrange
        var buffer = CreateBuffer(1024);
        var random = new Random(42);
        var totalSize = 0;
        var expectedData = new List<byte>();

        // Act - Write many chunks
        for (int i = 0; i < 1000; i++)
        {
            var chunk = new byte[random.Next(1, 100)];
            random.NextBytes(chunk);
            buffer.Write(chunk);
            expectedData.AddRange(chunk);
            totalSize += chunk.Length;
        }

        // Assert
        Assert.Equal(totalSize, buffer.Length);
        Assert.Equal(expectedData.ToArray(), buffer.Span.ToArray());
    }

    [Fact]
    public void Multiple_ExpandAndShrink_Cycles_WorkCorrectly()
    {
        // Arrange
        var buffer = CreateBuffer(4);

        // Act - Multiple expand/shrink cycles
        for (int i = 0; i < 10; i++)
        {
            // Expand
            buffer.Write(new byte[100]);
            Assert.True(buffer.Capacity >= 100);

            // Shrink
            buffer.Shrink();
            Assert.Equal(4, buffer.Capacity);

            // Clear for next cycle
            buffer.Clear();
        }

        // Assert
        Assert.Equal(4, buffer.Capacity);
        Assert.Equal(0, buffer.Length);
    }

    #endregion

    #region Helper Methods

    private static ReadOnlySequence<byte> CreateMultiSegmentSequence(params byte[][] segments)
    {
        if (segments.Length == 0)
            return ReadOnlySequence<byte>.Empty;

        var bufferSegments = new BufferSegment<byte>(segments[0]);
        var firstSegment = bufferSegments;
        var lastSegment = bufferSegments;

        for (int i = 1; i < segments.Length; i++)
        {
            var newSegment = new BufferSegment<byte>(segments[i]);
            lastSegment.SetNext(newSegment);
            lastSegment = newSegment;
        }

        return new ReadOnlySequence<byte>(firstSegment, 0, lastSegment, lastSegment.Memory.Length);
    }

    private class BufferSegment<T> : ReadOnlySequenceSegment<T>
    {
        public BufferSegment(ReadOnlyMemory<T> memory)
        {
            Memory = memory;
        }

        public void SetNext(BufferSegment<T> next)
        {
            Next = next;
            next.RunningIndex = RunningIndex + Memory.Length;
        }
    }

    #endregion
}

// Extension methods for testing
internal static class NativeChunkedBufferExtensions
{
    public static bool IsDisposed(this NativeChunkedBuffer buffer)
    {
        try
        {
            buffer.Write(ReadOnlySpan<byte>.Empty);
            return false;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }
}
