using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class MessageEnumerator : IMessageEnumerator
{
    public async IAsyncEnumerable<IMessageBuffer> EnumerateAsync(
        IMessageReceiver messageReceiver, ConnectionContext context, WsMiddlewareOptions options, 
        IAsyncObjectPool<IMessageBuffer> bufferPool, [EnumeratorCancellation] CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var messageBuffer = await bufferPool.Rent(token);

            await messageReceiver.Receive(context, messageBuffer, token);

            yield return messageBuffer;
        }
    }
}
