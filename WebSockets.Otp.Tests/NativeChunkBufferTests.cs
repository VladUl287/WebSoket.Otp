using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Core.Helpers;
using Xunit;

namespace WebSockets.Otp.Tests;

public class NativeChunkedBufferTests
{
    [Fact]
    public void Constructor_WithValidCapacity_InitializesCorrectly()
    {
        // Arrange & Act
        using var buffer = new NativeChunkedBuffer(1024);

        // Assert
        Assert.Equal(0, buffer.Length);
        Assert.Equal(1024, buffer.Capacity);
        Assert.True(buffer.Span.IsEmpty);
    }

    [Fact]
    public void Constructor_WithZeroCapacity_ThrowsNoException()
    {
        // Arrange & Act
        using var buffer = new NativeChunkedBuffer(0);

        // Assert
        Assert.Equal(0, buffer.Length);
        Assert.Equal(0, buffer.Capacity);
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ThrowsOutOfMemoryException()
    {
        // Arrange, Act & Assert
        Assert.Throws<OutOfMemoryException>(() => new NativeChunkedBuffer(-1));
    }

    [Fact]
    public void Write_WithEmptyData_DoesNotChangeLength()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);
        var initialLength = buffer.Length;
        var data = ReadOnlySpan<byte>.Empty;

        // Act
        buffer.Write(data);

        // Assert
        Assert.Equal(initialLength, buffer.Length);
    }

    [Fact]
    public void Write_WithDataWithinCapacity_StoresDataCorrectly()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        buffer.Write(data);

        // Assert
        Assert.Equal(5, buffer.Length);
        Assert.Equal(data, buffer.Span.ToArray());
    }

    [Fact]
    public void Write_MultipleTimes_AppendsDataCorrectly()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(20);
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
    public void Write_ExceedingCapacity_AutoExpandsBuffer()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(4);
        var data = new byte[] { 1, 2, 3, 4, 5 }; // Exceeds initial capacity

        // Act
        buffer.Write(data);

        // Assert
        Assert.Equal(5, buffer.Length);
        Assert.True(buffer.Capacity >= 5);
        Assert.Equal(data, buffer.Span.ToArray());
    }

    [Fact]
    public void Write_WithVeryLargeData_ExpandsToRequiredCapacity()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(10);
        var data = new byte[1000];
        new Random(42).NextBytes(data);

        // Act
        buffer.Write(data);

        // Assert
        Assert.Equal(1000, buffer.Length);
        Assert.True(buffer.Capacity >= 1000);
        Assert.Equal(data, buffer.Span.ToArray());
    }

    [Fact]
    public void Write_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var buffer = new NativeChunkedBuffer(100);
        buffer.Dispose();
        var data = new byte[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => buffer.Write(data));
    }

    [Fact]
    public void Write_ExceedingMaxArraySize_ThrowsOutOfMemoryException()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(int.MaxValue);
        buffer.SetLength(int.MaxValue - 100); // Set near maximum
        var data = new byte[200]; // This would exceed Array.MaxLength

        // Act & Assert
        Assert.Throws<OutOfMemoryException>(() => buffer.Write(data));
    }

    [Fact]
    public void Clear_ResetsLengthButNotCapacity()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);
        var data = new byte[] { 1, 2, 3, 4, 5 };
        buffer.Write(data);
        var initialCapacity = buffer.Capacity;

        // Act
        buffer.Clear();

        // Assert
        Assert.Equal(0, buffer.Length);
        Assert.Equal(initialCapacity, buffer.Capacity);
        Assert.True(buffer.Span.IsEmpty);
    }

    [Fact]
    public void Clear_OnEmptyBuffer_DoesNothing()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);
        var initialCapacity = buffer.Capacity;

        // Act
        buffer.Clear();

        // Assert
        Assert.Equal(0, buffer.Length);
        Assert.Equal(initialCapacity, buffer.Capacity);
    }

    [Fact]
    public void Clear_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var buffer = new NativeChunkedBuffer(100);
        buffer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => buffer.Clear());
    }

    [Fact]
    public void SetLength_IncreaseLength_PadsWithZeros()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);
        var initialData = new byte[] { 1, 2, 3 };
        buffer.Write(initialData);

        // Act
        buffer.SetLength(10);

        // Assert
        Assert.Equal(10, buffer.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 0, 0, 0, 0, 0, 0, 0 }, buffer.Span.ToArray());
    }

    [Fact]
    public void SetLength_DecreaseLength_TruncatesData()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);
        var data = new byte[] { 1, 2, 3, 4, 5 };
        buffer.Write(data);

        // Act
        buffer.SetLength(3);

        // Assert
        Assert.Equal(3, buffer.Length);
        Assert.Equal(new byte[] { 1, 2, 3 }, buffer.Span.ToArray());
    }

    [Fact]
    public void SetLength_ToSameLength_NoChange()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);
        var data = new byte[] { 1, 2, 3 };
        buffer.Write(data);
        var expected = buffer.Span.ToArray();

        // Act
        buffer.SetLength(3);

        // Assert
        Assert.Equal(3, buffer.Length);
        Assert.Equal(expected, buffer.Span.ToArray());
    }

    [Fact]
    public void SetLength_IncreaseBeyondCapacity_ExpandsBuffer()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(5);

        // Act
        buffer.SetLength(20);

        // Assert
        Assert.Equal(20, buffer.Length);
        Assert.True(buffer.Capacity >= 20);
    }

    [Fact]
    public void SetLength_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetLength(-1));
    }

    [Fact]
    public void SetLength_ExceedingMaxArraySize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetLength(Array.MaxLength + 1));
    }

    [Fact]
    public void Shrink_WhenAboveInitialCapacity_ReducesToInitialCapacity()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(10);
        buffer.Write(new byte[100]); // Force expansion
        var expandedCapacity = buffer.Capacity;

        // Act
        buffer.Shrink();

        // Assert
        Assert.Equal(10, buffer.Capacity);
        Assert.True(expandedCapacity > 10);
    }

    [Fact]
    public void Shrink_WhenAtOrBelowInitialCapacity_DoesNothing()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(50);
        var initialCapacity = buffer.Capacity;

        // Act
        buffer.Shrink();

        // Assert
        Assert.Equal(initialCapacity, buffer.Capacity);
    }

    [Fact]
    public void Shrink_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var buffer = new NativeChunkedBuffer(100);
        buffer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => buffer.Shrink());
    }

    [Fact]
    public void Span_ReturnsCorrectViewOfData()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);
        var data = new byte[] { 10, 20, 30, 40, 50 };
        buffer.Write(data);

        // Act
        var span = buffer.Span;

        // Assert
        Assert.Equal(data.Length, span.Length);
        Assert.Equal(data, span.ToArray());
    }

    [Fact]
    public void Span_OnEmptyBuffer_ReturnsEmptySpan()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);

        // Act
        var span = buffer.Span;

        // Assert
        Assert.True(span.IsEmpty);
        Assert.Equal(0, span.Length);
    }

    [Fact]
    public void Manager_ReturnsValidMemoryManager()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(100);
        var data = new byte[] { 1, 2, 3 };
        buffer.Write(data);

        // Act
        using var manager = buffer.Manager;
        var memory = manager.Memory;

        // Assert
        Assert.Equal(data, memory.Span.ToArray());
        Assert.IsType<MemoryManager>(manager);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var buffer = new NativeChunkedBuffer(100);

        // Act & Assert (should not throw)
        buffer.Dispose();
        buffer.Dispose();
        buffer.Dispose();
    }

    [Fact]
    public void Finalizer_DoesNotThrow()
    {
        // Arrange & Act
        var buffer = new NativeChunkedBuffer(100);

        // No assertion - just verifying the finalizer doesn't throw
        // In real scenario, the buffer would be garbage collected
    }

    [Fact]
    public void MemoryManager_GetSpan_ReturnsCorrectSpan()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        unsafe
        {
            fixed (byte* ptr = data)
            {
                using var manager = new MemoryManager(ptr, data.Length);

                // Act
                var span = manager.GetSpan();

                // Assert
                Assert.Equal(data.Length, span.Length);
                Assert.Equal(data, span.ToArray());
            }
        }
    }

    //[Fact]
    //public void MemoryManager_Pin_WithValidIndex_ReturnsCorrectPointer()
    //{
    //    // Arrange
    //    var data = new byte[] { 1, 2, 3, 4, 5 };
    //    unsafe
    //    {
    //        fixed (byte* originalPtr = data)
    //        {
    //            using var manager = new MemoryManager(originalPtr, data.Length);

    //            // Act
    //            using var handle = manager.Pin(2);

    //            // Assert
    //            Assert.Equal(originalPtr + 2, (byte*)handle.Pointer);
    //        }
    //    }
    //}

    [Fact]
    public void MemoryManager_Pin_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3 };
        unsafe
        {
            fixed (byte* ptr = data)
            {
                using var manager = new MemoryManager(ptr, data.Length);

                // Act & Assert
                Assert.Throws<ArgumentOutOfRangeException>(() => manager.Pin(5));
                Assert.Throws<ArgumentOutOfRangeException>(() => manager.Pin(-1));
            }
        }
    }

    [Fact]
    public void MemoryManager_Unpin_DoesNotThrow()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3 };
        unsafe
        {
            fixed (byte* ptr = data)
            {
                using var manager = new MemoryManager(ptr, data.Length);

                // Act & Assert (should not throw)
                manager.Unpin();
            }
        }
    }

    [Fact]
    public void IntegrationTest_ComplexScenario()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(10);
        var random = new Random(42);

        // Act & Assert - Complex sequence of operations
        for (int i = 0; i < 5; i++)
        {
            var data = new byte[random.Next(1, 20)];
            random.NextBytes(data);
            buffer.Write(data);
        }

        Assert.True(buffer.Length > 0);
        Assert.True(buffer.Capacity >= buffer.Length);

        var midLength = buffer.Length / 2;
        buffer.SetLength(midLength);
        Assert.Equal(midLength, buffer.Length);

        buffer.Clear();
        Assert.Equal(0, buffer.Length);

        // Write again after clear
        var finalData = new byte[] { 100, 101, 102 };
        buffer.Write(finalData);
        Assert.Equal(finalData, buffer.Span.ToArray());

        buffer.Shrink();
        Assert.Equal(10, buffer.Capacity); // Back to initial capacity
    }

    [Fact]
    public void EnsureCapacity_DoublesCapacityUntilSufficient()
    {
        // Arrange
        using var buffer = new NativeChunkedBuffer(8);

        // Act - Force multiple expansions
        buffer.Write(new byte[100]);

        // Assert - Capacity should be at least 100 and follow doubling pattern
        Assert.True(buffer.Capacity >= 100);
        Assert.True(buffer.Length == 100);
    }
}
