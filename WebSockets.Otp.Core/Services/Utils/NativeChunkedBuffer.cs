using System.Buffers;
using System.Runtime.InteropServices;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.Utils;

public sealed unsafe class NativeChunkedBuffer(int capacity) : MemoryManager<byte>, IMessageBuffer
{
    private byte* _buffer = (byte*)NativeMemory.AllocZeroed((uint)capacity);

    private readonly int _initialCapacity = capacity;
    private int _capacity = capacity;
    private int _length;
    private bool _disposed;

    public int Length => _length;
    public int Capacity => _capacity;

    public Span<byte> Span => new(_buffer, _length);

    public void Write(ReadOnlySequence<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_length > Array.MaxLength - data.Length)
            throw new OutOfMemoryException("The combined length would exceed maximum array size.");

        var dataLength = (int)data.Length;

        if (_length > _capacity - dataLength)
            EnsureCapacity(_length + dataLength);

        var target = new Span<byte>(_buffer + _length, dataLength);
        data.CopyTo(target);

        _length += dataLength;
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
        ObjectDisposedException.ThrowIf(_disposed, this);

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
        ObjectDisposedException.ThrowIf(_disposed, this);

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
        ObjectDisposedException.ThrowIf(_disposed, this);

        void* newPtr = NativeMemory.Realloc(_buffer, (uint)capacity);
        if (newPtr is null)
            throw new OutOfMemoryException("Realloc failed.");

        _buffer = (byte*)newPtr;
        _capacity = capacity;
        _length = Math.Min(_length, capacity);
    }

    public override Span<byte> GetSpan() => new(_buffer, _length);

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        if ((uint)elementIndex >= (uint)_length)
            throw new ArgumentOutOfRangeException(nameof(elementIndex));

        return new MemoryHandle(_buffer + elementIndex);
    }

    public override void Unpin()
    { }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (_buffer is not null)
        {
            NativeMemory.Free(_buffer);
            _buffer = null;
        }

        if (disposing)
        {
            _capacity = 0;
            _length = 0;
        }

        _disposed = true;
    }
}
