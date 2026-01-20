using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Enums;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageProcessor
{
    ProcessingMode Mode { get; }

    Task Process(
        IMessageEnumerator enumerator, IGlobalContext globalContext, ISerializer serializer,
        WsBaseConfiguration options, CancellationToken token);
}
