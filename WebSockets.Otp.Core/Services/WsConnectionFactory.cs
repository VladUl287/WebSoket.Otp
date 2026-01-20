using System.IO.Pipelines;
using WebSockets.Otp.Core.Models;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Transport;

namespace WebSockets.Otp.Core.Services;

public sealed class WsConnectionFactory(IIdProvider idProvider) : IWsConnectionFactory
{
    public IWsConnection Create(IConnectionTransport transport) =>
        new WsConnection(idProvider.Create(), transport);

    public IConnectionTransport CreateTransport(IDuplexPipe duplexPipe, ISerializer serializer) =>
        new DuplexPipeTransport(duplexPipe, serializer);
}
