using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Pipeline;
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
        var options = new WsOptions();

        services.AddSingleton<IAsyncObjectPool<IMessageBuffer>>(
            (_) => new AsyncObjectPool<IMessageBuffer>(
                options.MessageBufferPoolSize,
                () => new NativeChunkedBuffer(options.MessageBufferCapacity)
            )
        );

        services.AddSingleton(options);

        //Trasnport
        services.AddSingleton<IMessageEnumeratorFactory, MessageEnumeratorFactory>();

        services.AddMainServices(options);
        return services.AddEndpointServices(options, assemblies);
    }

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, Action<WsOptions> configure, params Assembly[] assemblies)
    {
        var options = new WsOptions();
        configure(options);

        services.AddMainServices(options);
        return services.AddEndpointServices(options, assemblies);
    }

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, WsOptions options, params Assembly[] assemblies)
    {
        services.AddMainServices(options);
        services.AddEndpointServices(options, assemblies);
        return services;
    }

    private static IServiceCollection AddMainServices(this IServiceCollection services, WsOptions options)
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
        services.AddSingleton<IMessageReceiverResolver, MessageReaderResolver>();
        services.AddSingleton<IMessageReader, JsonMessageReader>();
        services.AddSingleton<IMessageProcessor, ParallelMessageProcessor>();

        services.AddSingleton<IWsRequestHandler, DefaultRequestHandler>();

        services.AddSingleton<IHandshakeService>(
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
        services.AddSingleton<IEndpointInvokerFactory, EndpointInvokerFactory>();

        services.AddSingleton<IMessageBufferFactory, MessageBufferFactory>();
        services.AddSingleton<IPipelineFactory, PipelineFactory>();
        services.AddSingleton<IMessageDispatcher, MessageDispatcher>();

        services.AddSingleton<IMessageProcessorResolver, MessageProcessorResolver>();

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

    private static IServiceCollection AddEndpointServices(this IServiceCollection services, WsOptions options, params Assembly[] assemblies)
    {
        var endpointsTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsWsEndpoint());

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
                ServiceLifetime.Singleton => services.AddKeyedSingleton(serviceType: typeof(IWsEndpoint), key, implementationType: endpointType),
                ServiceLifetime.Transient => services.AddKeyedSingleton(serviceType: typeof(IWsEndpoint), key, implementationType: endpointType),
                _ => services.AddKeyedScoped(serviceType: typeof(IWsEndpoint), key, implementationType: endpointType)
            };
        }

        services.AddSingleton<IStringPool>(new PreloadedStringPool(endpointsKeys, Encoding.UTF8));

        return services;
    }
}
