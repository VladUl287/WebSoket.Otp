namespace WebSockets.Otp.Abstractions.Contracts;

public interface IEndpointInvoker
{
    Task InvokeEndpointAsync(object endpointInstance, IEndpointExecutionContext ctx, CancellationToken ct);
}
