using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts.Transport;

public interface IMessageEnumerator
{
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        IMessageReceiver receiver, ConnectionContext context, WsMiddlewareOptions options, CancellationToken token);
}
