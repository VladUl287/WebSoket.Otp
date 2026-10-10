using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace WebSockets.Otp.Abstractions.Endpoints;

public sealed class WsEndpointInfo
{
    public required string Key { get; init; }
    public required Type EndpointType { get; init; }
    public required IEndpointInvoker Invoker { get; init; }

#if NET9_0_OR_GREATER
    private readonly Lock _gate = new();
#else
    private readonly object _gate = new();
#endif

    private Task<AuthorizationPolicy?>? _policyTask;
    private AuthorizationPolicy? _policy;
    private volatile bool _policySet;

    public Endpoint? Endpoint { get; init; }
    public AuthorizationPolicy? Policy => _policy;
    public bool IsPolicySet => _policySet;

    public ValueTask<AuthorizationPolicy?> GetOrComputePolicy(
        Func<WsEndpointInfo, IServiceProvider, Task<AuthorizationPolicy?>> factory, IServiceProvider provider)
    {
        if (_policySet) return ValueTask.FromResult(_policy);

        lock (_gate)
        {
            _policyTask ??= RunAsync(factory, provider);

            return new ValueTask<AuthorizationPolicy?>(_policyTask);
        }
    }

    private async Task<AuthorizationPolicy?> RunAsync(
        Func<WsEndpointInfo, IServiceProvider, Task<AuthorizationPolicy?>> factory, IServiceProvider provider)
    {
        var policy = await factory(this, provider);
        _policy = policy;
        _policySet = true;
        return policy;
    }
}
