namespace WebSockets.Otp.Abstractions.Utils;

public interface ITrieResolver<T>
{
    T Resolve(byte[] sequence);
    T Resolve(ReadOnlySpan<byte> sequence);
}
