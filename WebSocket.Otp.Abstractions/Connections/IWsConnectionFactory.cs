using System.IO.Pipelines;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Abstractions.Connections;

public interface IWsConnectionFactory
{
    IWsConnection Create(IConnectionTransport transport);
    IConnectionTransport CreateTransport(IDuplexPipe duplexPipe, ISerializer serializer);
}
