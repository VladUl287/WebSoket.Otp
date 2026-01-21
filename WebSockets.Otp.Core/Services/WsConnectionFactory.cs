using System.Net.WebSockets;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Services;

public sealed class WsConnectionFactory(IIdProvider idProvider) : IWsConnectionFactory
{
    public IWsConnection Create(WebSocket socket, ISerializer serializer) =>
        new WsConnection(idProvider.Create(), socket, serializer);
}
