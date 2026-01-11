using Microsoft.AspNetCore.Connections;

namespace WebSockets.Otp.Abstractions.Contracts.Transport;

public interface IMessageReceiver
{
    ValueTask Receive(ConnectionContext context, IMessageBuffer buffer, CancellationToken token);
}
