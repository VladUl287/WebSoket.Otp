using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IHandshakeService
{
    Task<WsHandshakeOptions?> GetOptions(ConnectionContext context, CancellationToken token);
}
