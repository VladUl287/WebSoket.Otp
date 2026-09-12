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
using WebSockets.Otp.Abstractions.Serializers;
using WebSockets.Otp.Abstractions.Transport;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Processors;
using WebSockets.Otp.Core.Services;
using WebSockets.Otp.Core.Services.Endpoints;
using WebSockets.Otp.Core.Services.IdProviders;
using WebSockets.Otp.Core.Services.Serializers;
using WebSockets.Otp.Core.Services.Utils;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, Action<WsOptions> configure, Assembly[] assemblies)
    {
        var options = new WsOptions();
        configure(options);
        return services.AddWsEndpointsCore(options, assemblies);
    }

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, WsOptions options, Assembly[] assemblies) =>
        services.AddWsEndpointsCore(options, assemblies);

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, Assembly[] assemblies) =>
        services.AddWsEndpointsCore(new(), assemblies);

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, Action<WsOptions> configure) =>
        services.AddWsEndpoints(configure, [Assembly.GetCallingAssembly()]);

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, WsOptions options) =>
        services.AddWsEndpoints(options, [Assembly.GetCallingAssembly()]);

    public static IServiceCollection AddWsEndpoints(this IServiceCollection services) =>
        services.AddWsEndpoints([Assembly.GetCallingAssembly()]);

    public static IServiceCollection AddJsonSerializer(this IServiceCollection services, Action<JsonSerializerOptions> configure)
    {
        var jsonOptions = new JsonSerializerOptions();
        configure(jsonOptions);
        return services.AddSingleton<ISerializer>(new JsonMessageSerializer(jsonOptions));
    }

    private static IServiceCollection AddWsEndpointsCore(this IServiceCollection services, WsOptions options, Assembly[] assemblies)
    {
        services.AddSingleton(options);

        services.AddTransport();
        services.AddDefaultSerializers();
        services.AddCoreServices();
        services.AddConnectionServices();
        services.AddUtility(options);
        services.AddEndpoints(options, assemblies);

        return services;
    }

    private static IServiceCollection AddTransport(this IServiceCollection services)
    {
        services.AddSingleton<IMessageEnumerator, MessageEnumerator>();
        services.AddSingleton<IMessageBufferFactory, MessageBufferFactory>();
        services.AddSingleton<IMessageProcessor, ParallelMessageProcessor>();
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

    private static IServiceCollection AddUtility(this IServiceCollection services, WsOptions options)
    {
        services.AddSingleton<IAsyncObjectPool<IMessageBuffer>>(
            (_) => new AsyncObjectPool<IMessageBuffer>(
                options.BufferPoolSize,
                () => new NativeChunkedBuffer(options.ReceiveBufferSize)
            )
        );
        services.AddSingleton<IIdProvider, GuidIdProvider>();
        return services;
    }

    private static IServiceCollection AddEndpoints(this IServiceCollection services, WsOptions config, params Assembly[] assemblies)
    {
        services.AddSingleton<IEndpointInvokerFactory, DefaultInvokerFactory>();
        services.AddSingleton<IContextFactory, DefaultContextFactory>();

        var endpointsTypes = assemblies
            .SelectMany(assembly => assembly
                .GetTypes()
                .Where(type => type.IsWsEndpoint())
            );

        var endpointsKeysBytes = new List<byte[]>();
        var endpointsKeys = new HashSet<string>();
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
                ServiceLifetime.Singleton => services.AddSingleton(endpointType),
                ServiceLifetime.Scoped => services.AddScoped(endpointType),
                _ => services.AddTransient(endpointType),
            };

            endpointsKeysBytes.Add(Encoding.UTF8.GetBytes(endpointKey));
        }

        services.AddSingleton<ITrieResolver>(new EndpointTypeResolver([.. endpointsKeysBytes], [.. endpointsTypes]));
        return services;
    }
}
