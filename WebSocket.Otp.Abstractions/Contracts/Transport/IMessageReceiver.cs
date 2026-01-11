using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts.Transport;

public interface IMessageReceiver
{
    ValueTask<IMessageBuffer> Receive(ConnectionContext context, WsMiddlewareOptions options, CancellationToken token);
}
