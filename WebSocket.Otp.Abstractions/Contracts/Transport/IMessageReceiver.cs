using Microsoft.AspNetCore.Connections;

namespace WebSockets.Otp.Abstractions.Contracts.Transport;

public interface IMessageReceiver
{
    string Protocol { get; }

    ValueTask Receive(ConnectionContext context, IMessageBuffer buffer, CancellationToken token);
}
