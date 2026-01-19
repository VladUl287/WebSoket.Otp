using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageProcessor
{
    string ProcessingMode { get; }

    Task Process(
        IMessageEnumerator enumerator, IGlobalContext globalContext, ISerializer serializer, 
        WsBaseOptions options, CancellationToken token);
}
