using System.Buffers;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IStringPool
{
    string Get(ReadOnlySpan<byte> bytes);
    string Get(ReadOnlySequence<byte> bytes);
}
