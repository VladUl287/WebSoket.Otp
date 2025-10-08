using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WebSockets.Otp.Core.Helpers;

public sealed unsafe class NativeChunkBuffer(int capacity) : IDisposable
{
    private byte* _buffer = (byte*)NativeMemory.AllocZeroed((uint)capacity);

    private readonly int _initialCapacity = capacity;
    private int _capacity = capacity;
    private int _position;
    private bool _disposed;

    public void Write(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();
    }

    public void Shrink()
    {
        ThrowIfDisposed();

        if (_capacity <= _initialCapacity)
            return;

        void* newPtr = NativeMemory.Realloc(_buffer, (uint)_initialCapacity);
        if (newPtr is null)
            throw new OutOfMemoryException("Realloc failed.");

        _buffer = (byte*)newPtr;
        _capacity = _initialCapacity;
        _position = _initialCapacity;
    }

    public void Clear()
    {
        ThrowIfDisposed();

        NativeMemory.Clear(_buffer, (uint)_capacity);
        _position = 0;
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
