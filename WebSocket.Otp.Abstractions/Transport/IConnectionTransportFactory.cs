using System.IO.Pipelines;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IConnectionTransportFactory
{
    IConnectionTransport Create(IDuplexPipe duplexPipe, ISerializer serializer);
}
