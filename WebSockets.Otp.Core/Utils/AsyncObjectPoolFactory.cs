using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Utils;

public sealed class AsyncObjectPoolFactory : IAsyncObjectPoolFactory
{
    public IAsyncObjectPool<TObject> Create<TObject>(int capacity, Func<TObject> factory) where TObject : notnull
    {
        return new AsyncObjectPool<TObject>(capacity, factory);
    }
}
