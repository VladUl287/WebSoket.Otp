using System.Text;
using System.Reflection;
using WebSockets.Otp.Core.Services;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Attributes;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Core.Services.Authorization;
using WebSockets.Otp.Core.Services.IdProviders;
using WebSockets.Otp.Core.Services.MessageProcessors;
using WebSockets.Otp.Core.Services.Serializers;
using WebSockets.Otp.Core.Services.Validators;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Extensions;

public static class WsServiceCollectionsExtensions
{
    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddMainServices();
        services.AddEndpointServices(new WsGlobalOptions(), assemblies);
        return services;
    }

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, Action<WsGlobalOptions> configure, params Assembly[] assemblies)
    {
        var globalOptions = new WsGlobalOptions();
        configure(globalOptions);

        services.AddMainServices();
        services.AddEndpointServices(globalOptions, assemblies);
        return services;
    }

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, WsGlobalOptions options, params Assembly[] assemblies)
    {
        services.AddMainServices();
        services.AddEndpointServices(options, assemblies);
        return services;
    }

    private static IServiceCollection AddMainServices(this IServiceCollection services)
    {
        services.AddCoreServices();
        services.AddConnectionServices();
        services.AddMessageProcessingServices();
        services.AddSerializationServices();
        services.AddUtilityServices();
        return services;
    }

    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IWsAuthorizationService, WsAuthorizationService>();
        services.AddSingleton<IWsEndpointRegistry, WsEndpointRegistry>();
        services.AddSingleton<IWsService, WsService>();

        services.AddSingleton<IHandshakeRequestParser>(
            new DefaultHandshakeRequestParser(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }));
        services.AddSingleton<IHandshakeRequestProcessor, DefaultHandshakeRequestProcessor>();

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
        services.AddSingleton<ISerializerResolver, DefaultSerializerResolver>();
        services.AddSingleton<ISerializer, JsonMessageSerializer>();

        return services;
    }

    private static IServiceCollection AddUtilityServices(this IServiceCollection services)
    {
        services.AddSingleton<IIdProvider, GuidIdProvider>();

        return services;
    }

    private static IServiceCollection AddEndpointServices(this IServiceCollection services, WsGlobalOptions options, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(assemblies);

        services.AddSingleton(options);

        var endpointsTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsWsEndpoint());

        services.AddSingleton<IWsEndpointRegistry>(new WsEndpointRegistry(endpointsTypes));

        var comparer = options.KeyOptions.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var endpointsKeys = new HashSet<string>(comparer);
        foreach (var endpointType in endpointsTypes)
        {
            var attribute = endpointType.GetCustomAttribute<WsEndpointAttribute>() ??
                throw new InvalidOperationException($"Type {endpointType.Name} is missing WsEndpointAttribute");

            var key = attribute.Validate(options.KeyOptions).Key;

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
}
