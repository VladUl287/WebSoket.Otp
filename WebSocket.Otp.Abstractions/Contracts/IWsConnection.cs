using Microsoft.AspNetCore.Http;
using WebSockets.Otp.Abstractions.Contracts.Transport;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnection : IDisposable
{
    string Id { get; }

    HttpContext Context { get; }

    IWsTransport Transport { get; }
}
