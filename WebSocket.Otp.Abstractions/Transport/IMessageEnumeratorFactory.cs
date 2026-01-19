using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageEnumeratorFactory
{
    IMessageEnumerator Create(ConnectionContext context, IMessageReceiver receiver, IAsyncObjectPool<IMessageBuffer> bufferPool);
}
