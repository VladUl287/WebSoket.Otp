using Microsoft.AspNetCore.Connections;
using System.Runtime.CompilerServices;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Contracts.Transport;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class MessageEnumerator(
    IMessageReceiver messageReceiver, IMessageBufferFactory bufferFactory) : IMessageEnumerator
{
    public async IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        ConnectionContext context, WsMiddlewareOptions options, [EnumeratorCancellation] CancellationToken token)
    {
        await using var bufferPool = new AsyncObjectPool<int, IMessageBuffer>(
            options.Memory.MaxBufferPoolSize, bufferFactory.Create);

        while (!token.IsCancellationRequested)
        {
            var messageBuffer = await bufferPool.Rent(options.Memory.InitialBufferSize);

            await messageReceiver.Receive(context, messageBuffer, token);

            yield return messageBuffer;
        }
    }
}
