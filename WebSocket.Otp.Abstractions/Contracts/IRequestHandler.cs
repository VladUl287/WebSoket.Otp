using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Configuration;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IRequestHandler
{
    Task HandleRequestAsync(HttpContext context, WsBaseOptions options);
}
