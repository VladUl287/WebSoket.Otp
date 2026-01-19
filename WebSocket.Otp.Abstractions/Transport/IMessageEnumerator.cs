using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageEnumerator
{
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(IMessageBufferFactory bufferFactory, CancellationToken token);
    IAsyncEnumerable<IMessageBuffer> EnumerateAsync(IAsyncObjectPool<IMessageBuffer> bufferPool, CancellationToken token);
}
