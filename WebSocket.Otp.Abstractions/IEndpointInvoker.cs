using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Abstractions;

public interface IEndpointInvoker
{
    Task InvokeEndpointAsync(object endpointInstance, IWsExecutionContext ctx, CancellationToken ct);
}
