using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace WebSockets.Otp.Core.Utils;

public sealed class AsyncObjectPool<TState, TObject>(int size, Func<TState, TObject> objectFactory) : IAsyncDisposable
{
    private readonly Channel<TObject> _channel = Channel.CreateBounded<TObject>(
        new BoundedChannelOptions(size)
        {
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = false,
            Capacity = size,
        });

    private int _disposed;
    private int _created;
    private readonly Lock _creationLock = new();

    public ValueTask<TObject> Rent(TState state, CancellationToken token = default)
    {
        ThrowIfDisposed();

        if (_channel.Reader.TryRead(out var obj))
            return new ValueTask<TObject>(obj);

        if (Volatile.Read(ref _created) >= size)
            return _channel.Reader.ReadAsync(token);

        lock (_creationLock)
        {
            if (_created < size)
            {
                _created++;
                return new ValueTask<TObject>(objectFactory(state));
            }
        }

        return _channel.Reader.ReadAsync(token);
    }

    public ValueTask Return(TObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ThrowIfDisposed();

        return _channel.Writer.WriteAsync(obj);
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
