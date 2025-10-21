namespace WebSockets.Otp.Abstractions;

public interface IRequestState<T>
{
    string GenerateKey();
    public T Get(string key);
    public void Save(string key, T state);
    public void Remove(string key);
}
