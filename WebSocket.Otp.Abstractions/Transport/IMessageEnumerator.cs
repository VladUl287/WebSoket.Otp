using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageEnumerator
{
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        IMessageReceiver receiver, ConnectionContext context, WsMiddlewareOptions options, 
        IAsyncObjectPool<IMessageBuffer> objectPool, CancellationToken token);
}
