namespace WebSockets.Otp.Abstractions;

public interface IRequestState<T>
{
    string GenerateKey();
    public Task<T> Get(string key);
    public Task Save(string key, T state, CancellationToken token);
    public Task Remove(string key);
}
