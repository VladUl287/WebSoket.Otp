using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Pipeline;
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Pipeline;
using WebSockets.Otp.Core.Services;
using WebSockets.Otp.Core.Services.Endpoints;
using WebSockets.Otp.Core.Services.IdProviders;
using WebSockets.Otp.Core.Services.Processors;
using WebSockets.Otp.Core.Services.Serializers;
using WebSockets.Otp.Core.Services.Transport;
using WebSockets.Otp.Core.Services.Validators;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        var options = new WsConfiguration();

        services.AddSingleton<IAsyncObjectPool<IMessageBuffer>>(
            (_) => new AsyncObjectPool<IMessageBuffer>(
                options.MessageBufferPoolSize,
                () => new NativeChunkedBuffer(options.MessageBufferCapacity)
            )
        );

        services.AddSingleton(options);

        services.AddTransport();

        services.AddMainServices(options);

        return services.AddEndpointServices(options, assemblies);
    }

    private static IServiceCollection AddTransport(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBufferFactory, MessageBufferFactory>();
        services.AddSingleton<IMessageEnumeratorFactory, MessageEnumeratorFactory>();
        services.AddSingleton<IMessageProcessor, ParallelMessageProcessor>();
        services.AddSingleton<IMessageProcessorStore, MessageProcessorStore>();
        services.AddSingleton<IMessageReader, JsonMessageReader>();
        services.AddSingleton<IMessageReaderStore, MessageReaderStore>();
        return services;
    }

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, Action<WsConfiguration> configure, params Assembly[] assemblies)
    {
        var options = new WsConfiguration();
        configure(options);

        services.AddMainServices(options);
        return services.AddEndpointServices(options, assemblies);
    }

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, WsConfiguration options, params Assembly[] assemblies)
    {
        services.AddMainServices(options);
        services.AddEndpointServices(options, assemblies);
        return services;
    }

    private static IServiceCollection AddMainServices(this IServiceCollection services, WsConfiguration options)
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
        services.AddSingleton<IMessageReaderStore, MessageReaderStore>();
        services.AddSingleton<IMessageReader, JsonMessageReader>();
        services.AddSingleton<IMessageProcessor, ParallelMessageProcessor>();

        services.AddSingleton<IRequestHandler, DefaultRequestHandler>();

        services.AddSingleton<IHandshakeService, HandshakeService>();

        services.AddSingleton<IRequestHandler, DefaultRequestHandler>();
        return services.AddSingleton<IContextFactory, ExecutionContextFactory>();
    }

    private static IServiceCollection AddConnectionServices(this IServiceCollection services)
    {
        services.AddSingleton<IWsConnectionManager, InMemoryConnectionManager>();
        return services.AddSingleton<IWsConnectionFactory, WsConnectionFactory>();
    }

    private static IServiceCollection AddMessageProcessingServices(this IServiceCollection services)
    {
        services.AddSingleton<IEndpointInvokerFactory, EndpointInvokerFactory>();

        services.AddSingleton<IMessageBufferFactory, MessageBufferFactory>();
        services.AddSingleton<IPipelineFactory, PipelineFactory>();
        services.AddSingleton<IMessageDispatcher, MessageDispatcher>();

        return services;
    }

    private static IServiceCollection AddSerializationServices(this IServiceCollection services)
    {
        services.AddSingleton<ISerializerStore, DefaultSerializerStore>();
        return services.AddSingleton<ISerializer, JsonMessageSerializer>();
    }

    private static IServiceCollection AddUtilityServices(this IServiceCollection services)
    {
        return services.AddSingleton<IIdProvider, GuidIdProvider>();
    }

    private static IServiceCollection AddEndpointServices(this IServiceCollection services, WsConfiguration options, params Assembly[] assemblies)
    {
        var endpointsTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsWsEndpoint());

        var endpointsKeys = new HashSet<string>(options.Endpoint.Comparer);

        foreach (var endpointType in endpointsTypes)
        {
            var attribute = endpointType.GetCustomAttribute<WsEndpointAttribute>() ??
                throw new InvalidOperationException($"Type {endpointType.Name} is missing WsEndpointAttribute");

            var endpointKey = attribute.Validate(options.Endpoint).Key;

            if (!endpointsKeys.Add(endpointKey))
                throw new InvalidOperationException($"Duplicate WsEndpoint key detected: {endpointKey} in type {endpointType.Name}");

            var serviceType = typeof(IWsEndpoint);
            _ = attribute.Scope switch
            {
                ServiceLifetime.Singleton => services.AddKeyedSingleton(serviceType, endpointKey, endpointType),
                ServiceLifetime.Scoped => services.AddKeyedScoped(serviceType, endpointKey, endpointType),
                _ => services.AddKeyedTransient(serviceType, endpointKey, endpointType),
            };
        }

        services.AddSingleton<IStringPool>(
            new EndpointsKeysPool(endpointsKeys, Encoding.UTF8, options.Endpoint.UnsafeInternKeys));

        return services;
    }
}
