using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services.Utils;

public sealed class EndpointValidator(IServiceScopeFactory factory) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        using var scope = factory.CreateScope();

        var endpoints = scope.ServiceProvider
            .GetKeyedServices<IWsEndpoint>(KeyedService.AnyKey);

        var options = scope.ServiceProvider
            .GetRequiredService<WsConfiguration>();

        foreach (var endpoint in endpoints)
        {
            var attribute = endpoint
                .GetType()
                .GetCustomAttribute<WsEndpointAttribute>();
        }

        return next;
    }
}
