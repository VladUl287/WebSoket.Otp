namespace WebSockets.Otp.Abstractions.Utils;

public interface ITrieResolver<T>
{
    T Resolve(ReadOnlySpan<byte> sequence);
}
