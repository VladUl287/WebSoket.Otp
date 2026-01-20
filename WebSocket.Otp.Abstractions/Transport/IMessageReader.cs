using Microsoft.AspNetCore.Connections;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageReader
{
    string ProtocolName { get; }

    ValueTask Receive(ConnectionContext context, IMessageBuffer buffer, CancellationToken token);
}
