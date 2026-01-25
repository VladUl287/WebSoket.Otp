using System.Buffers;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageBuffer : IDisposable
{
    public int Length { get; }
    public int Capacity { get; }

    Span<byte> Span { get; }
    Memory<byte> Memory { get; }

    void Write(ReadOnlySpan<byte> data);
    void Write(ReadOnlySequence<byte> data);
    void SetLength(int length);
    void Shrink();
}
