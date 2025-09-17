using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWsFramework(this IServiceCollection services, params Assembly[] assembliesToScan)
    {
        services.AddSingleton<IClock, UtcClock>();
        services.AddSingleton<IIdProvider, GuidIdProvider>();
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
        services.AddSingleton<IWsConnectionManager, InMemoryConnectionManager>();
        services.AddSingleton<IWsEndpointRegistry, WsEndpointRegistry>();
        services.AddSingleton<EndpointInvoker>();
        services.AddScoped<IMessageDispatcher, MessageDispatcher>();
        //services.AddScoped<MessageDispatcher>();
        //services.AddSingleton<IMessageDispatcher>(sp => sp.GetRequiredService<MessageDispatcher>());

        foreach (var asm in assembliesToScan)
        {
            var types = asm.GetTypes().Where(t => !t.IsAbstract && t.GetCustomAttribute<WsEndpointAttribute>() != null);
            foreach (var t in types)
            {
                // register endpoint in DI (scoped)
                services.AddScoped(t);
                // register in registry on startup (we'll resolve registry and call Register later)
                // but we can't call registry here because container building not finished. We'll add hosted service to register at runtime.
            }
        }

        services.AddSingleton<IStartupRegister, DefaultStartupRegistrar>();
        return services;
    }
}