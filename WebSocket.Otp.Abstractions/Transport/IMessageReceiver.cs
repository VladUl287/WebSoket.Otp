using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageReceiver
{
    string ProtocolName { get; }

    ValueTask Receive(ConnectionContext context, IMessageBuffer buffer, CancellationToken token);
}
