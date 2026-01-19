using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageReceiverResolver
{
    bool TryResolve(ProcessProtocol protocol, [NotNullWhen(true)] out IMessageReader? messageReceiver);
}
