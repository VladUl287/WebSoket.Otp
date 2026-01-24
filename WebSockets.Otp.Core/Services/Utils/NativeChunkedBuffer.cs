using System.Buffers;
using System.Runtime.InteropServices;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.Utils;

public sealed unsafe class NativeChunkedBuffer(int capacity) : IMessageBuffer
{
    private byte* _buffer = (byte*)NativeMemory.AllocZeroed((uint)capacity);

    private readonly int _initialCapacity = capacity;
    private int _capacity = capacity;
    private int _length;
    private bool _disposed;

    public int Length => _length;
    public int Capacity => _capacity;

    public ReadOnlySpan<byte> Span => new Span<byte>(_buffer, _length);
    public IMemoryOwner<byte> Manager => new MemoryManager(_buffer, _length);

    public void Write(ReadOnlySequence<byte> data)
    {
        foreach (var part in data)
            Write(part.Span);
    }

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

        NativeMemory.Clear(_buffer, (uint)_length);
        _length = 0;
    }

    public void SetLength(int length)
    {
        if (length < 0 || length > Array.MaxLength)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (length > _capacity)
            EnsureCapacity(length);

        if (length > _length)
            new Span<byte>(_buffer + _length, length - _length).Clear();

        _length = length;
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

        NativeMemory.Free(_buffer);
        _buffer = null;
        _capacity = 0;
        _length = 0;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~NativeChunkedBuffer()
    {
        if (_buffer is not null)
            NativeMemory.Free(_buffer);
    }
}

public sealed unsafe class MemoryManager(byte* pointer, int length) : MemoryManager<byte>
{
    public override Span<byte> GetSpan() => new(pointer, length);

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        if ((uint)elementIndex >= (uint)length)
            throw new ArgumentOutOfRangeException(nameof(elementIndex));

        return new MemoryHandle(pointer + elementIndex);
    }

    public override void Unpin() { }

    protected override void Dispose(bool disposing) { }
}