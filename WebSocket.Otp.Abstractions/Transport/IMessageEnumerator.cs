using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageEnumerator
{
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        WebSocket socket, WsOptionsSnapshot config, IAsyncObjectPool<IMessageBuffer> pool, CancellationToken token);
}
