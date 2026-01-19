using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageEnumerator
{
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        ConnectionContext context, IMessageReceiver receiver, IAsyncObjectPool<IMessageBuffer> objectPool,
        CancellationToken token);

    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(CancellationToken token);
}
