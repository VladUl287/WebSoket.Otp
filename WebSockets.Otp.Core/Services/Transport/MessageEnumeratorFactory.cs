using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class MessageEnumeratorFactory(WsConfiguration options) : IMessageEnumeratorFactory
{
    public IMessageEnumerator Create(ConnectionContext context, IMessageReader receiver) =>
        new MessageEnumerator(context, receiver, options);
}
