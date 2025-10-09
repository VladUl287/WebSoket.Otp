using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WebSockets.Otp.Core.Helpers;

public sealed unsafe class NativeChunkBuffer(int capacity) : IDisposable
{
    private byte* _buffer = (byte*)NativeMemory.AllocZeroed((uint)capacity);

    private readonly int _initialCapacity = capacity;
    private int _capacity = capacity;
    private int _length;
    private bool _disposed;

    public ReadOnlySpan<byte> Data => new Span<byte>(_buffer, _length);

    public void Write(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_length > Array.MaxLength - data.Length)
            throw new OutOfMemoryException("The combined length would exceed maximum array size.");

        if (_length > _capacity - data.Length)
            EnsureCapacity(_length + data.Length);

        var target = new Span<byte>(_buffer + _length, data.Length);
        data.CopyTo(target);

        _length += data.Length;
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _capacity)
            return;

        var newCapacity = _capacity == 0 ? 4 : _capacity * 2;
        if ((uint)newCapacity > Array.MaxLength)
            newCapacity = Array.MaxLength;
        newCapacity = Math.Max(newCapacity, requiredCapacity);
        Reallocate(newCapacity);
    }

    public void Shrink()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_capacity <= _initialCapacity)
            return;

        Reallocate(_initialCapacity);
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        NativeMemory.Clear(_buffer, (uint)_capacity);
        _length = 0;
    }

    private void Reallocate(int capacity)
    {
        void* newPtr = NativeMemory.Realloc(_buffer, (uint)capacity);
        if (newPtr is null)
            throw new OutOfMemoryException("Realloc failed.");

        _buffer = (byte*)newPtr;
        _capacity = capacity;
        _length = Math.Min(_length, capacity);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        NativeMemory.Free(_buffer);
        GC.SuppressFinalize(this);
    }

    ~NativeChunkBuffer()
    {
        Dispose();
    }
}
