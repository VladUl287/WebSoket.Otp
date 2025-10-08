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
        ThrowIfDisposed();

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

        //TODO: udpate to 4 → 8 → 16 → 32 → 64 → 128 growth
        var newCapacity = _capacity == 0 ? 4 : _capacity * 2;
        newCapacity = Math.Max(newCapacity, requiredCapacity);
        Reallocate(newCapacity);
    }

    public void Shrink()
    {
        ThrowIfDisposed();

        if (_capacity <= _initialCapacity)
            return;

        Reallocate(_initialCapacity);
    }

    public void Clear()
    {
        ThrowIfDisposed();

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, "Native chunk buffer already disposed");

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
