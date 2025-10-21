using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.AspNet.Authorization;
using WebSockets.Otp.AspNet.Middlewares;
using WebSockets.Otp.Core;
using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Core.Helpers;

namespace WebSockets.Otp.AspNet.Extensions;

public static class WsMiddlewareExtensions
{
    public static IServiceCollection AddWsEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<IWsAuthorizationService, WsAuthorizationService>();
        services.AddSingleton<IWsAuthorizationValidator, AuthenticationValidator>();
        services.AddSingleton<IWsAuthorizationValidator, PolicyValidator>();
        services.AddSingleton<IWsAuthorizationValidator, RoleValidator>();
        services.AddSingleton<IWsAuthorizationValidator, SchemeValidator>();

        services.AddSingleton<IWsEndpointRegistry, WsEndpointRegistry>();
        services.AddHostedService((sp) => new WsEndpointInitializer(sp, assemblies));

        services.AddSingleton<IMessageBufferFactory, MessageBufferFactory>();
        services.AddSingleton<IWsService, WsService>();

        services.AddSingleton<IExecutionContextFactory, ExecutionContextFactory>();

        services.AddSingleton<IMethodResolver, DefaultMethodResolver>();
        services.AddSingleton<IEndpointInvoker, EndpointInvoker>();

        services.AddSingleton<IClock, UtcClock>();
        services.AddSingleton<IIdProvider, GuidIdProvider>();
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
        services.AddSingleton<IWsConnectionManager, InMemoryConnectionManager>();
        services.AddSingleton<IWsConnectionFactory, WsConnectionFactory>();
        services.AddSingleton<IMessageDispatcher, MessageDispatcher>();

        services.AddSingleton<IRequestState<WsConnectionOptions>, RequestState>();

        services.AddEndpoints(assemblies);

        return services;
    }

    private static void AddEndpoints(this IServiceCollection services, Assembly[] assemblies)
    {
        var endpointsTypes = assemblies.GetEndpoints();
        foreach (var endpointType in endpointsTypes)
            services.AddScoped(endpointType);
    }

    public static IApplicationBuilder UseWsEndpoints(this IApplicationBuilder builder, Action<WsMiddlewareOptions> configure)
    {
        var options = new WsMiddlewareOptions();
        configure(options);
        options.RequestMatcher ??= new PathWsRequestMatcher(options.RequestPath);
        options.HandshakeRequestMatcher ??= new HandshakeRequestMatcher(options.HandshakeRequestPath);
        return builder.UseMiddleware<WsMiddleware>(options);
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
