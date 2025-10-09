using System.Buffers;

namespace WebSockets.Otp.Abstractions;

public interface IMessageBuffer : IDisposable
{
    ReadOnlySpan<byte> Span { get; }
    IMemoryOwner<byte> Manager { get; }
}
