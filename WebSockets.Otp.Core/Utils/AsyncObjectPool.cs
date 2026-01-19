using System.Runtime.CompilerServices;
using System.Threading.Channels;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Utils;

public sealed class AsyncObjectPool<TObject>(int capacity, Func<TObject> factory) : IAsyncObjectPool<TObject>
    where TObject : notnull
{
    private readonly Channel<TObject> _channel = Channel.CreateBounded<TObject>(
        new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = false,
            Capacity = capacity,
        });

    private int _disposed;
    private int _created;
    private readonly Lock _creationLock = new();

    public ValueTask<TObject> Rent(CancellationToken token = default)
    {
        ThrowIfDisposed();

        if (_channel.Reader.TryRead(out var obj))
            return new ValueTask<TObject>(obj);

        if (Volatile.Read(ref _created) >= capacity)
            return _channel.Reader.ReadAsync(token);

        lock (_creationLock)
        {
            if (_created < capacity)
            {
                _created++;
                return new ValueTask<TObject>(factory());
            }
        }

        return _channel.Reader.ReadAsync(token);
    }

    public ValueTask Return(TObject obj, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ThrowIfDisposed();

        return _channel.Writer.WriteAsync(obj, token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed == 1, this);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            return;

        _channel.Writer.Complete();

        while (_channel.Reader.TryRead(out var obj))
        {
            if (obj is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (obj is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
