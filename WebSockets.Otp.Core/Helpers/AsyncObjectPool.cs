using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace WebSockets.Otp.Core.Helpers;

public sealed class AsyncObjectPool<T>(int size, Func<T> objectFactory) : IAsyncDisposable
{
    private readonly Channel<T> _channel = Channel.CreateBounded<T>(
        new BoundedChannelOptions(size)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
        });

    private int _disposed;
    private int _initialized;

    public async ValueTask Initilize()
    {
        ThrowIfDisposed();

        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            return;

        for (int i = 0; i < size; i++)
        {
            var obj = objectFactory.Invoke();
            await _channel.Writer.WriteAsync(obj);
        }
    }

    public ValueTask<T> Rent()
    {
        ThrowIfDisposed();
        ThrowIfNotInitilized();
        return _channel.Reader.ReadAsync();
    }

    public ValueTask Return(T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ThrowIfDisposed();
        ThrowIfNotInitilized();

        return _channel.Writer.WriteAsync(obj);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed == 1, this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfNotInitilized()
    {
        if (_initialized == 0) throw new InvalidOperationException();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            return;

        _channel.Writer.Complete();

        while (_channel.Reader.TryRead(out var obj))
        {
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
                continue;
            }
            if (obj is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
