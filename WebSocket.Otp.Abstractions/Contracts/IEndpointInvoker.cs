namespace WebSockets.Otp.Abstractions.Contracts;

public interface IEndpointInvoker
{
    Task InvokeEndpointAsync(object endpointInstance, IWsExecutionContext ctx, CancellationToken ct);
}
