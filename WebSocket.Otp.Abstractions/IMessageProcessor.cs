using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Abstractions;

public interface IMessageProcessor
{
    string Name { get; }
    Task Process(IWsConnection connection, WsMiddlewareOptions options);
}
