using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IRequestHandler
{
    Task HandleRequestAsync(ConnectionContext context, WsBaseOptions options);
}
