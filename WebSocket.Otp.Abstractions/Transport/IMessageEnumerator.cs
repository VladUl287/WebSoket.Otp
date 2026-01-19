using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageEnumerator
{
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(CancellationToken token);
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(IMessageBufferFactory bufferFactory, CancellationToken token);
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(IAsyncObjectPool<IMessageBuffer> bufferPool, CancellationToken token);
}
