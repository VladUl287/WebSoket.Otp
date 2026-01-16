using System.IO.Pipelines;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services;

public sealed class DuplexPipeTransport(IDuplexPipe duplexPipe) : IWsTransport
{
    public void Dispose() { }
}
