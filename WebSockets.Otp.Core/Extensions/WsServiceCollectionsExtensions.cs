using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Pipeline;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Core.Pipeline;
using WebSockets.Otp.Core.Services;
using WebSockets.Otp.Core.Services.Endpoints;
using WebSockets.Otp.Core.Services.IdProviders;
using WebSockets.Otp.Core.Services.MessageProcessors;
using WebSockets.Otp.Core.Services.Serializers;
using WebSockets.Otp.Core.Services.Transport;
using WebSockets.Otp.Core.Services.Validators;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Extensions;

public static class WsServiceCollectionsExtensions
{
    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        var options = new WsGlobalOptions();
        services.AddMainServices(options);
        return services.AddEndpointServices(options, assemblies);
    }

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, Action<WsGlobalOptions> configure, params Assembly[] assemblies)
    {
        var options = new WsGlobalOptions();
        configure(options);

        services.AddMainServices(options);
        return services.AddEndpointServices(options, assemblies);
    }

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, WsGlobalOptions options, params Assembly[] assemblies)
    {
        services.AddMainServices(options);
        services.AddEndpointServices(options, assemblies);
        return services;
    }

    private static IServiceCollection AddMainServices(this IServiceCollection services, WsGlobalOptions options)
    {
        services.AddCoreServices();
        services.AddConnectionServices();
        services.AddMessageProcessingServices();
        services.AddSerializationServices();
        services.AddUtilityServices();
        return services.AddSingleton(options);
    }

    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IMessageReceiverResolver, MessageReceiverResolver>();
        services.AddSingleton<IMessageEnumerator, MessageEnumerator>();
        services.AddSingleton<IMessageReceiver, JsonMessageReceiver>();
        services.AddSingleton<INewMessageProcessor, NewParallelMessageProcessor>();

        services.AddSingleton<IWsEndpointRegistry, EndpointRegistry>();
        services.AddSingleton<IWsRequestHandler, DefaultRequestHandler>();

        services.AddSingleton<IHandshakeParser>(
            new HandshakeRequestParser(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }));

        services.AddSingleton<IWsRequestHandler, DefaultRequestHandler>();
        return services.AddSingleton<IExecutionContextFactory, ExecutionContextFactory>();
    }

    private static IServiceCollection AddConnectionServices(this IServiceCollection services)
    {
        services.AddSingleton<IWsConnectionManager, InMemoryConnectionManager>();
        return services.AddSingleton<IWsConnectionFactory, WsConnectionFactory>();
    }

    private static IServiceCollection AddMessageProcessingServices(this IServiceCollection services)
    {
        services.AddSingleton<IEndpointInvoker, EndpointInvoker>();
        services.AddSingleton<IEndpointInvokerFactory, EndpointInvokerFactory>();

        services.AddSingleton<IMessageBufferFactory, MessageBufferFactory>();
        services.AddSingleton<IPipelineFactory, PipelineFactory>();
        services.AddSingleton<IMessageDispatcher, MessageDispatcher>();

        services.AddSingleton<IMessageProcessorFactory, MessageProcessorFactory>();

        return services;
    }

    private static IServiceCollection AddSerializationServices(this IServiceCollection services)
    {
        services.AddSingleton<ISerializerResolver, DefaultSerializerResolver>();
        return services.AddSingleton<ISerializer, JsonMessageSerializer>();
    }

    private static IServiceCollection AddUtilityServices(this IServiceCollection services)
    {
        return services.AddSingleton<IIdProvider, GuidIdProvider>();
    }

    private static IServiceCollection AddEndpointServices(this IServiceCollection services, WsGlobalOptions options, params Assembly[] assemblies)
    {
        var endpointsTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsWsEndpoint());

        services.AddSingleton<IWsEndpointRegistry>(new EndpointRegistry(endpointsTypes));

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
