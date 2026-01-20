using System.Diagnostics.CodeAnalysis;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageReaderStore
{
    bool TryGet(string protocol, [NotNullWhen(true)] out IMessageReader? messageReceiver);
}
