namespace WebSockets.Otp.Core.Helpers;

public sealed class NativeChunkBuffer : IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    ~NativeChunkBuffer()
    {
        Dispose();
    }
}
