using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Utils;

public sealed class TrieResolver<T> : ITrieResolver<T>
{
    public T Resolve(byte[] sequence)
    {
        return Resolve(sequence.AsSpan());
    }

    public T Resolve(ReadOnlySpan<byte> sequence)
    {
        throw new NotImplementedException();
    }
}
