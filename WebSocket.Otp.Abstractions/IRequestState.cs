namespace WebSockets.Otp.Abstractions;

public interface IRequestState<T>
{
    string GenerateKey();
    public T Get(string key);
    public Task Save(string key, T state, CancellationToken token);
    public void Remove(string key);
}
