using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Abstractions.Endpoints;

public interface IGlobalContext
{
    HttpContext Context { get; }
    string ConnectionId { get; }
    GroupManager Groups { get; }
}
