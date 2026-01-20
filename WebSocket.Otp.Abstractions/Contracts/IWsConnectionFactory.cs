using System.IO.Pipelines;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IWsConnectionFactory
{
    IWsConnection Create(IConnectionTransport transport);
    IConnectionTransport CreateTransport(IDuplexPipe duplexPipe, ISerializer serializer);
}
