using System.Diagnostics.CodeAnalysis;

namespace WebSockets.Otp.Abstractions.Utils;

public interface ITrieResolver<T>
{
    bool TryResolve(ReadOnlyMemory<byte> sequence, [NotNullWhen(true)] out T? value);
}
