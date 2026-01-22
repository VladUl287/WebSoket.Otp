using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Middlewares;

namespace WebSockets.Otp.Core.Extensions;

public static class EndpointExtensions
{
    public static WsEndpointConventionBuilder MapWsEndpoints(
        this IEndpointRouteBuilder builder, string pattern, Action<WsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var options = new WsOptions();
        configure?.Invoke(options);

        if (configure is null)
            options = builder.ServiceProvider.GetRequiredService<WsGlobalOptions>();

        var app = builder.CreateApplicationBuilder();
        app.UseWebSockets(options.WebSocketOptions);
        app.Run((httpContext) =>
        {
            var requestProcessor = httpContext.RequestServices.GetRequiredService<IRequestHandler>();
            return requestProcessor.HandleRequestAsync(httpContext, options);
        });
        var executeHandler = app.Build();

        var executeBuilder = builder
            .Map(pattern, executeHandler)
            .DisableRequestTimeout()
            .DisableAntiforgery()
            .RequireCors();

        executeBuilder.Add(builder =>
        {
            foreach (var data in options.AuthorizationData)
                builder.Metadata.Add(data);
        });

        return new WsEndpointConventionBuilder(executeBuilder);
    }
}
