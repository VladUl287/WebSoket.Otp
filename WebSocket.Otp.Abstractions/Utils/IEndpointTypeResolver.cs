namespace WebSockets.Otp.Abstractions.Utils;

public interface ITrieResolver
{
    Type Resolve(byte[] sequence);
    Type Resolve(ReadOnlySpan<byte> sequence);
}
