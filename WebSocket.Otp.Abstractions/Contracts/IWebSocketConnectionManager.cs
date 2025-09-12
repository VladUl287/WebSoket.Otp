using System.Net.WebSockets;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWebSocketConnectionManager
{
    string Add(WebSocket webSocket);

    bool Remove(string connectionId);

    WebSocket Get(string connectionId);

    string GetId(WebSocket webSocket);

    IEnumerable<WebSocket> GetAll();
}
