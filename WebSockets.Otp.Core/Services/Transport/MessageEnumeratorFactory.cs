using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services.Transport;

public sealed class MessageEnumeratorFactory(WsOptions options) : IMessageEnumeratorFactory
{
    public IMessageEnumerator Create(ConnectionContext context, IMessageReceiver receiver) =>
        new MessageEnumerator(context, receiver, options);
}
