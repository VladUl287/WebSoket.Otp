using System.Runtime.InteropServices;

namespace WebSockets.Otp.Core.Helpers;

public sealed unsafe class NativeChunkBuffer : IDisposable
{
    private readonly byte* _buffer;
    private readonly int _initialCapacity;
    private bool _disposed;

    public NativeChunkBuffer(int capacity)
    {
        _initialCapacity = capacity;
        _buffer = (byte*)NativeMemory.AllocZeroed((uint)capacity);
    }

    public void Write(ReadOnlySpan<byte> data)
    {

    }

    public void Shrink()
    {
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
