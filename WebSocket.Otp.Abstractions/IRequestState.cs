namespace WebSockets.Otp.Abstractions;

public interface IRequestState<T>
{
    string GenerateKey();
    public Task<T> GetAsync(string key);
    public Task SaveAsync(string key, T state, CancellationToken token);
    public Task RevokeAsync(string key);
}
