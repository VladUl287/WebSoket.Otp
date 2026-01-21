using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageEnumerator
{
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        WebSocket socket, WsBaseOptions config, IMessageBufferFactory factory, CancellationToken token);
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        WebSocket socket, WsBaseOptions config, IAsyncObjectPool<IMessageBuffer> pool, CancellationToken token);
}
