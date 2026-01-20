using WebSockets.Otp.Abstractions.Enums;

namespace WebSockets.Otp.Abstractions.Transport;

public interface IMessageProcessorStore
{
    IMessageProcessor Get(ProcessingMode mode);
}
