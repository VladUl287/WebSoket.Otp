using System.IO.Pipelines;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class WsConnectionFactory(IIdProvider idProvider) : IWsConnectionFactory
{
    public IWsConnection Create(IConnectionTransport transport) =>
        new WsConnection(idProvider.Create(), transport);

    public IConnectionTransport CreateTransport(IDuplexPipe duplexPipe, ISerializer serializer) =>
        new DuplexPipeTransport(duplexPipe, serializer);
}
