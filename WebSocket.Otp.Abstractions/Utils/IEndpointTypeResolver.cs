using System.Diagnostics.CodeAnalysis;

namespace WebSockets.Otp.Abstractions.Utils;

public interface ITrieResolver<T>
{
    bool TryResolve(ReadOnlySpan<byte> sequence, [NotNullWhen(true)] out T? value);
}
