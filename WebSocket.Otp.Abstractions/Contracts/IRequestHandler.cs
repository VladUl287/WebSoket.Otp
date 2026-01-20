using Microsoft.AspNetCore.Connections;
using WebSockets.Otp.Abstractions.Configuration;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IRequestHandler
{
    Task HandleRequestAsync(ConnectionContext context, WsBaseConfiguration options);
}
