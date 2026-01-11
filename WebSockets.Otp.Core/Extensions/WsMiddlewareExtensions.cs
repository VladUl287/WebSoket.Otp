using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WebSockets.Otp.Core.Middlewares;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace WebSockets.Otp.Core.Extensions;

public static class WsMiddlewareExtensions
{
    public static WsEndpointConventionBuilder MapWsEndpoints(this IEndpointRouteBuilder builder, Action<WsMiddlewareOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        var options = new WsMiddlewareOptions();
        configure(options);

        builder
            .Map(options.HandshakePath, (context) =>
            {
                return context.RequestServices
                    .GetRequiredService<IHandshakeRequestProcessor>()
                    .HandleRequestAsync(context, options);
            })
            ;

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

        if (options.Authorization is not null)
            conventionBuilder.WithMetadata(options.Authorization);

        return new WsEndpointConventionBuilder(conventionBuilder);
    }
}
