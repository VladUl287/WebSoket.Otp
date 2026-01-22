using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageEnumerator
{
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        WebSocket socket, WsOptions config, IMessageBufferFactory factory, CancellationToken token);
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        WebSocket socket, WsOptions config, IAsyncObjectPool<IMessageBuffer> pool, CancellationToken token);
}
