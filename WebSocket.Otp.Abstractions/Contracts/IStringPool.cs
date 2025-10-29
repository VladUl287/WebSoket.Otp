using System.Buffers;
using System.Text;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IStringPool
{
    Encoding Encoding { get; }
    string Intern(ReadOnlySpan<byte> bytes, bool unsafeMode = false);
    string Intern(ReadOnlySequence<byte> bytes, bool unsafeMode = false);
}
