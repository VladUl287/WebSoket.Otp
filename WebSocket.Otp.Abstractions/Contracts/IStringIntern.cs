using System.Buffers;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IStringIntern
{
    string Intern(ReadOnlySpan<byte> bytes);
    string Intern(ReadOnlySequence<byte> bytes);
}
