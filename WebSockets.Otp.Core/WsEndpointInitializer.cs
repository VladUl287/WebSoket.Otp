using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Extensions;

namespace WebSockets.Otp.Core;

public sealed class WsEndpointInitializer(IServiceProvider sp, Assembly[] assemblies) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var registry = sp.GetRequiredService<IWsEndpointRegistry>();
        var enpointsTypes = assemblies.GetEndpoints();
        registry.Register(enpointsTypes);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
