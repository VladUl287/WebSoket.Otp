using Microsoft.Extensions.ObjectPool;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.Utils;

public sealed class NativeChunkedBufferPolicy(WsOptions options) : IPooledObjectPolicy<IMessageBuffer>
{
    public IMessageBuffer Create() => new NativeChunkedBuffer(options.ReceiveBufferSize);

    public bool Return(IMessageBuffer obj)
    {
        obj.SetLength(0);

        if (options.ShrinkBuffers)
            obj.Shrink();

        return true;
    }
}
