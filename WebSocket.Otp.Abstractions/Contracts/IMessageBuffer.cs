using System.Buffers;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMessageBuffer : IDisposable
{
    public int Length { get; }
    public int Capacity { get; }
    ReadOnlySpan<byte> Span { get; }
    IMemoryOwner<byte> Manager { get; }
    void Write(ReadOnlySpan<byte> data);
    void SetLength(int length);
    void Shrink();
}
