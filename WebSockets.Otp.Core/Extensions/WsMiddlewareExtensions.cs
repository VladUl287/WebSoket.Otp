using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Middlewares;

namespace WebSockets.Otp.Core.Extensions;

public static class WsMiddlewareExtensions
{
    public static WsEndpointConventionBuilder MapWsEndpoints(this IEndpointRouteBuilder builder,
        Action<WsMiddlewareOptions> configure, Action<HttpConnectionDispatcherOptions>? configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var options = new WsMiddlewareOptions();
        configure(options);

        var httpOptions = new HttpConnectionDispatcherOptions();
        configureOptions?.Invoke(httpOptions);

        var conventionBuilder = builder
            .Map(options.RequestPath, (context) =>
            {
                return context.RequestServices
                    .GetRequiredService<IWsRequestProcessor>()
                    .HandleRequestAsync(context, options);
            })
            .DisableAntiforgery()
            .RequireCors()
            ;

        //var conventionBuilder = builder
        //    .MapConnections(options.RequestPath, httpOptions, (context) =>
        //    {
        //        var requestProcessor = context.ApplicationServices.GetRequiredService<IWsRequestProcessor>();
        //        context.Use((next) => (context) => 
        //            requestProcessor.HandleRequestAsync(context, options));
        //    })
        //    .DisableAntiforgery()
        //    .RequireCors()
        //    ;

        if (options.Authorization is not null)
            conventionBuilder.WithMetadata(options.Authorization);

        return new WsEndpointConventionBuilder(conventionBuilder);
    }
}
