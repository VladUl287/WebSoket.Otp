using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.AspNet.Middlewares;
using WebSockets.Otp.AspNet.Options;
using WebSockets.Otp.Core;

namespace WebSockets.Otp.AspNet.Extensions;

public static class WsMiddlewareExtensions
{
    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, params Assembly[] assembliesToScan)
    {
        services.AddHostedService<WsEndpointInitializer>();

        services.AddSingleton<IClock, UtcClock>();
        services.AddSingleton<IIdProvider, GuidIdProvider>();
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
        services.AddSingleton<IWsConnectionManager, InMemoryConnectionManager>();
        services.AddSingleton<IWsEndpointRegistry, WsEndpointRegistry>();
        services.AddSingleton<IWsConnectionFactory, WsConnectionFactory>();
        services.AddSingleton<EndpointInvoker>();
        services.AddScoped<IMessageDispatcher, MessageDispatcher>();

        foreach (var asm in assembliesToScan)
        {
            var types = asm.GetTypes().Where(t => !t.IsAbstract && t.GetCustomAttribute<WsEndpointAttribute>() != null);
            foreach (var t in types)
            {
                services.AddScoped(t);
            }
        }

        services.AddSingleton<IStartupRegister, DefaultStartupRegistrar>();

        return services;
    }

    public static IApplicationBuilder UseWsEndpoints(this IApplicationBuilder builder, Action<WsMiddlewareOptions> configure)
    {
        var options = new WsMiddlewareOptions();
        options.RequestMatcher = new PathWsRequestMatcher(options.Path);
        configure(options);
        return builder.UseMiddleware<WsMiddleware>(options);
    }
}
