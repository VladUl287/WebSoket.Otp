using System.Diagnostics.CodeAnalysis;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageReceiverResolver
{
    bool TryResolve(string format, [NotNullWhen(true)] out IMessageReceiver? messageReceiver);
}
