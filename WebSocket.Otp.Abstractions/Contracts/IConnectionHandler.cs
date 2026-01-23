using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IConnectionHandler
{
    Task HandleAsync(HttpContext context, WsConfiguration config);
}
