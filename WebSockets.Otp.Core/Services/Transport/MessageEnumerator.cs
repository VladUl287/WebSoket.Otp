using Microsoft.AspNetCore.Connections;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class MessageEnumerator : IMessageEnumerator
{
    public async IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        ConnectionContext context, IMessageReceiver receiver, IAsyncObjectPool<IMessageBuffer> objectPool,
        [EnumeratorCancellation] CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var messageBuffer = await objectPool.Rent(token);

            await receiver.Receive(context, messageBuffer, token);

            yield return messageBuffer;
        }
    }

    public IAsyncEnumerable<IMessageBuffer> EnumerateAsync(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IMessageBuffer> EnumerateAsync(IMessageBufferFactory bufferFactory, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IMessageBuffer> EnumerateAsync(IAsyncObjectPool<IMessageBuffer> bufferPool, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
