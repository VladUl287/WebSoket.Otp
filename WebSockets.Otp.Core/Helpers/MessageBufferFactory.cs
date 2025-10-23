using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core.Helpers;

public sealed class MessageBufferFactory : IMessageBufferFactory
{
    public IMessageBuffer Create(int capacity) => new NativeChunkedBuffer(capacity);
}
