using Microsoft.AspNetCore.Connections;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class MessageEnumerator(ConnectionContext context, IMessageReader receiver, WsConfiguration options) : IMessageEnumerator
{
    public async IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        IMessageBufferFactory bufferFactory, [EnumeratorCancellation] CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var buffer = bufferFactory.Create(options.MessageBufferCapacity);
            await receiver.Receive(context, buffer, token);
            yield return buffer;
        }
    }

    public async IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        IAsyncObjectPool<IMessageBuffer> bufferPool, [EnumeratorCancellation] CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var buffer = await bufferPool.Rent(token);
            await receiver.Receive(context, buffer, token);
            yield return buffer;
        }
    }
}
