using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageProcessor
{
    Task Process(IGlobalContext context, ISerializer serializer, CancellationToken token);
}
