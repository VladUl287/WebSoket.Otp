using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Authorization;
using WebSockets.Otp.AspNet.Middlewares;
using WebSockets.Otp.Core;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Helpers;
using WebSockets.Otp.Core.Processors;

namespace WebSockets.Otp.AspNet.Extensions;

public static class WsMiddlewareExtensions
{
    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddCoreServices();
        services.AddConnectionServices();
        services.AddMessageProcessingServices();
        services.AddSerializationServices();
        services.AddUtilityServices();
        services.AddEndpointServices(assemblies);

        return services;
    }

    public static IApplicationBuilder UseWsEndpoints(this IApplicationBuilder builder, Action<WsMiddlewareOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var options = new WsMiddlewareOptions();
        configure(options);
        options.Paths.RequestMatcher ??= new DefaltWsRequestMatcher(options.Paths.RequestPath);
        options.Paths.HandshakeRequestMatcher ??= new DefaultHandshakeRequestMatcher(options.Paths.HandshakePath);

        options.Validate();

        return builder.UseMiddleware<WsMiddleware>(options);
    }

    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IWsAuthorizationService, WsAuthorizationService>();
        services.AddSingleton<IWsEndpointRegistry, WsEndpointRegistry>();
        services.AddSingleton<IWsService, WsService>();
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
        services.AddSingleton<IMethodResolver, DefaultMethodResolver>();
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

    private static IServiceCollection AddEndpointServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddHostedService((sp) => new WsEndpointInitializer(sp, assemblies));

        var endpointsTypes = assemblies.GetEndpoints();
        foreach (var endpointType in endpointsTypes)
            services.AddScoped(endpointType);

        return services;
    }

    public static IEnumerable<Type> GetEndpoints(this IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies, nameof(assemblies));

        foreach (var assembly in assemblies)
        {
            var types = assembly
                .GetTypes()
                .Where(t => t.IsWsEndpoint());

            foreach (var type in types)
            {
                yield return type;
            }
        }
    }
}
