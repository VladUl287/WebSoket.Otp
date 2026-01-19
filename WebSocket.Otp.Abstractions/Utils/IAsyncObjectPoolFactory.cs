namespace WebSockets.Otp.Abstractions.Utils;

public interface IAsyncObjectPoolFactory
{
    public IAsyncObjectPool<TObject> Create<TObject>(int capacity, Func<TObject> factory)
        where TObject : notnull;
}
