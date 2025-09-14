namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnectionManager
{
    bool TryAdd(IWsConnection connection);

    bool TryRemove(string connectionId);

    IWsConnection Get(string connectionId);

    IEnumerable<IWsConnection> GetAll();
}
