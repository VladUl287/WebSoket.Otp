using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IEndpointInvoker
{
    Task InvokeEndpointAsync(object endpointInstance, IEndpointContext ctx, CancellationToken ct);
}
