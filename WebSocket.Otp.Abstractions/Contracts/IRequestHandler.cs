using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IRequestHandler
{
    Task HandleRequestAsync(HttpContext context, WsOptions options);
}
