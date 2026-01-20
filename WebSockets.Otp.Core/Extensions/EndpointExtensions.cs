using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Middlewares;

namespace WebSockets.Otp.Core.Extensions;

public static class EndpointExtensions
{
    public static WsEndpointConventionBuilder MapWsEndpoints(this IEndpointRouteBuilder builder, string pattern,
        Action<WsBaseConfiguration> configure, Action<HttpConnectionDispatcherOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var options = new WsBaseConfiguration();
        configure(options);

        var httpOptions = new HttpConnectionDispatcherOptions();
        configureOptions?.Invoke(httpOptions);

        //var conventionBuilder = builder
        //    .Map(options.RequestPath, (context) =>
        //    {
        //        return context.RequestServices
        //            .GetRequiredService<IWsRequestProcessor>()
        //            .HandleRequestAsync(context, options);
        //    })
        //    .DisableAntiforgery()
        //    .RequireCors()
        //    ;

        var conventionBuilder = builder
            .MapConnections(pattern, httpOptions, (context) =>
            {
                var requestProcessor = context.ApplicationServices.GetRequiredService<IRequestHandler>();
                context.Use((next) => (context) =>
                    requestProcessor.HandleRequestAsync(context, options));
            })
            .DisableAntiforgery()
            .RequireCors()
            ;

        if (options.Authorization is not null)
            conventionBuilder.WithMetadata(options.Authorization);

        return new WsEndpointConventionBuilder(conventionBuilder);
    }
}
