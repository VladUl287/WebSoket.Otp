using Microsoft.AspNetCore.Connections;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageEnumeratorFactory
{
    IMessageEnumerator Create(ConnectionContext context, IMessageReader receiver);
}
