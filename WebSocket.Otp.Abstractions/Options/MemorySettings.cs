namespace WebSockets.Otp.Abstractions.Options;

public sealed class MemorySettings
{
    public int MaxMessageSize { get; set; } = 64 * 1024; // 64KB
    public int InitialBufferSize { get; set; } = 4 * 1024; // 4KB
    public bool ReclaimBuffersImmediately { get; set; } = true;
    public int MaxBufferPoolSize { get; set; } = 10;
}
