using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Reflection;
using System.Text;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Authorization;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Helpers;
using WebSockets.Otp.Core.IdProviders;
using WebSockets.Otp.Core.Middlewares;
using WebSockets.Otp.Core.Processors;
using WebSockets.Otp.Core.Validators;

namespace WebSockets.Otp.Core.Extensions;

public static class WsMiddlewareExtensions
{
    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddCoreServices();
        services.AddConnectionServices();
        services.AddMessageProcessingServices();
        services.AddSerializationServices();
        services.AddUtilityServices();
        services.AddEndpointServices(WsEndpointAttributeOptions.Default, assemblies);

        return services;
    }

    public static IApplicationBuilder UseWsEndpoints(this IApplicationBuilder builder, Action<WsMiddlewareOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var options = new WsMiddlewareOptions();
        configure(options);
        options.Validate();

        return builder.UseMiddleware<WsMiddleware>(options);
    }

    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IWsAuthorizationService, WsAuthorizationService>();
        services.AddSingleton<IWsEndpointRegistry, WsEndpointRegistry>();
        services.AddSingleton<IWsService, WsService>();
        services.AddSingleton<IHandshakeRequestProcessor, HandshakeRequestProcessor>();
        services.AddSingleton<IWsRequestProcessor, WsRequestProcessor>();
        services.AddSingleton<IExecutionContextFactory, ExecutionContextFactory>();

        return services;
    }

    private static IServiceCollection AddConnectionServices(this IServiceCollection services)
    {
        services.AddSingleton<IWsConnectionManager, InMemoryConnectionManager>();
        services.AddSingleton<IWsConnectionFactory, WsConnectionFactory>();
        services.AddSingleton<IConnectionStateService, InMemoryConnectionStateService>();

        return services;
    }

    private static IServiceCollection AddMessageProcessingServices(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBufferFactory, MessageBufferFactory>();
        services.AddSingleton<IHandleDelegateFactory, DefaultDelegateFactory>();
        services.AddSingleton<IEndpointInvoker, EndpointInvoker>();
        services.AddSingleton<IMessageDispatcher, MessageDispatcher>();

        services.AddSingleton<IMessageProcessorFactory, MessageProcessorFactory>();
        services.AddSingleton<IMessageProcessor, SequentialMessageProcessor>();
        services.AddSingleton<IMessageProcessor, ParallelMessageProcessor>();

        return services;
    }

    private static IServiceCollection AddSerializationServices(this IServiceCollection services)
    {
        services.AddSingleton<ISerializerFactory, SerializerFactory>();
        services.AddSingleton<ISerializer, JsonMessageSerializer>();

        return services;
    }

    private static IServiceCollection AddUtilityServices(this IServiceCollection services)
    {
        services.AddSingleton<IClock, UtcClock>();
        services.AddSingleton<IIdProvider, GuidIdProvider>();

        return services;
    }

    private static IServiceCollection AddEndpointServices(this IServiceCollection services, WsEndpointAttributeOptions options, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(assemblies);

        services.AddSingleton<IWsEndpointRegistry>(sp =>
        {
            var registry = new WsEndpointRegistry();
            var endpointsTypes = assemblies.GetEndpoints();
            registry.Register(endpointsTypes);
            return registry;
        });

        services.AddSingleton(options);

        var endpointsTypes = assemblies.GetEndpoints();
        var endpointsKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var endpointType in endpointsTypes)
        {
            var attribute = endpointType.GetCustomAttribute<WsEndpointAttribute>() ??
                throw new InvalidOperationException($"Type {endpointType.Name} is missing WsEndpointAttribute");

            var key = attribute.Validate(options).Key;

            if (!endpointsKeys.Add(attribute.Key))
                throw new InvalidOperationException($"Duplicate WsEndpoint key detected: {key} in type {endpointType.Name}");

            _ = attribute.Scope switch
            {
                ServiceLifetime.Singleton => services.AddSingleton(endpointType),
                ServiceLifetime.Transient => services.AddTransient(endpointType),
                _ => services.AddScoped(endpointType)
            };
        }
        services.AddSingleton<IStringPool>(new PreloadedStringPool(endpointsKeys, Encoding.UTF8));

        return services;
    }

    public static IEnumerable<Type> GetEndpoints(this IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsWsEndpoint());
    }
}
