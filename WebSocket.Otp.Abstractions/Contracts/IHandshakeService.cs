namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeService
{
    string Protocol { get; }

    ReadOnlyMemory<byte> ResponseBytes { get; }
}
