using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Connections;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Abstractions.Options;
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
using WebSockets.Otp.Core.Services.Utils;

namespace WebSockets.Otp.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, Action<WsGlobalOptions> configure, Assembly[] assemblies)
    {
        var options = new WsGlobalOptions();
        configure(options);

        return services.AddWsEndpointsCore(options, assemblies);
    }

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, WsGlobalOptions options, Assembly[] assemblies) =>
        services.AddWsEndpointsCore(options, assemblies);

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, Assembly[] assemblies) =>
        services.AddWsEndpointsCore(new(), assemblies);

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, Action<WsGlobalOptions> configure) =>
        services.AddWsEndpoints(configure, [Assembly.GetCallingAssembly()]);

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, WsGlobalOptions options) =>
        services.AddWsEndpoints(options, [Assembly.GetCallingAssembly()]);

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services) =>
        services.AddWsEndpoints([Assembly.GetCallingAssembly()]);

    public static IServiceCollection AddJsonSerializer(this IServiceCollection services, Action<JsonSerializerOptions> configure)
    {
        var jsonOptions = new JsonSerializerOptions();
        configure(jsonOptions);
        return services.AddSingleton<ISerializer>(new JsonMessageSerializer(jsonOptions));
    }


    private static IServiceCollection AddWsEndpointsCore(this IServiceCollection services, WsGlobalOptions options, Assembly[] assemblies)
    {
        var configuration = new WsConfiguration(options);

        services.AddSingleton(configuration);

        services.AddPipeline();
        services.AddTransport();
        services.AddDefaultSerializers();
        services.AddCoreServices();
        services.AddConnectionServices();
        services.AddUtility(configuration);
        services.AddEndpoints(configuration, assemblies);

        return services;
    }

    private static IServiceCollection AddTransport(this IServiceCollection services)
    {
        services.AddSingleton<IMessageEnumerator, MessageEnumerator>();
        services.AddSingleton<IMessageBufferFactory, MessageBufferFactory>();
        services.AddSingleton<IMessageProcessor, ParallelMessageProcessor>();
        services.AddSingleton<IMessageProcessor, SequentialMessageProcessor>();
        services.AddSingleton<IMessageProcessorStore, MessageProcessorStore>();
        return services;
    }

    private static IServiceCollection AddDefaultSerializers(this IServiceCollection services)
    {
        services.AddSingleton<ISerializerStore, DefaultSerializerStore>();

        services.AddJsonSerializer(options =>
        {
            options.PropertyNameCaseInsensitive = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.IgnoreReadOnlyProperties = false;
            options.IgnoreReadOnlyFields = true;
            options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.WriteIndented = false;
            options.AllowTrailingCommas = false;
            options.ReadCommentHandling = JsonCommentHandling.Skip;
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement;
        });

        return services;
    }

    private static IServiceCollection AddPipeline(this IServiceCollection services)
    {
        services.AddSingleton<IPipelineFactory, PipelineFactory>();
        return services;
    }

    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionHandler, DefaultConnectionHandler>();
        services.AddSingleton<IMessageDispatcher, DefaultMessageDispatcher>();
        services.AddSingleton<IHandshakeHandler, DefaultHandshakeHandler>();
        return services;
    }

    private static IServiceCollection AddConnectionServices(this IServiceCollection services)
    {
        services.AddSingleton<IWsConnectionManager, InMemoryConnectionManager>();
        services.AddSingleton<IWsConnectionFactory, DefaultConnectionFactory>();
        return services;
    }

    private static IServiceCollection AddUtility(this IServiceCollection services, WsConfiguration configuration)
    {
        services.AddSingleton<IAsyncObjectPool<IMessageBuffer>>(
            (_) => new AsyncObjectPool<IMessageBuffer>(
                configuration.BufferPoolSize,
                () => new NativeChunkedBuffer(configuration.ReceiveBufferSize)
            )
        );

        services.AddSingleton<IIdProvider, GuidIdProvider>();

        services.AddSingleton<IStartupFilter, EndpointValidator>();

        return services;
    }

    private static IServiceCollection AddEndpoints(this IServiceCollection services, WsConfiguration config, params Assembly[] assemblies)
    {
        services.AddSingleton<IEndpointInvokerFactory, DefaultInvokerFactory>();
        services.AddSingleton<IContextFactory, DefaultContextFactory>();

        var endpointsTypes = assemblies
            .SelectMany(assembly => assembly
                .GetTypes()
                .Where(type => type.IsWsEndpoint())
            );

        var endpointsKeys = new HashSet<string>(config.Keys.Comparer);
        foreach (var endpointType in endpointsTypes)
        {
            var attribute = endpointType.GetCustomAttribute<WsEndpointAttribute>() ??
                throw new InvalidOperationException($"Type {endpointType.Name} is missing WsEndpointAttribute");

            var endpointKey = attribute.Key;

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
            new EndpointsKeysPool(endpointsKeys, Encoding.UTF8, config.Keys.UnsafeIntern));

        return services;
    }
}
