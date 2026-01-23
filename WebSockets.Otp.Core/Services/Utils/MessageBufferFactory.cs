using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.Utils;

public sealed class MessageBufferFactory : IMessageBufferFactory
{
    public IMessageBuffer Create(int capacity) => new NativeChunkedBuffer(capacity);
}
