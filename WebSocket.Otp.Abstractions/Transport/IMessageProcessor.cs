using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageProcessor
{
    ProcessingMode Mode { get; }

    Task Process(
        IGlobalContext globalContext, ISerializer serializer,
        WsBaseConfiguration options, CancellationToken token);
}
