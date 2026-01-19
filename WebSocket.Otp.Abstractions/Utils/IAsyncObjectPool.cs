namespace WebSockets.Otp.Abstractions.Utils;

public interface IAsyncObjectPool<TObject> : IAsyncDisposable
    where TObject : notnull
{
    ValueTask<TObject> Rent(CancellationToken token = default);
    ValueTask Return(TObject obj, CancellationToken token = default);
}
