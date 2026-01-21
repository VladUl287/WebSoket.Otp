using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions.Configuration;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Middlewares;

namespace WebSockets.Otp.Core.Extensions;

public static class EndpointExtensions
{
    //Action<HttpConnectionDispatcherOptions>? configureOptions = null
    public static WsEndpointConventionBuilder MapWsEndpoints(
        this IEndpointRouteBuilder builder, string pattern, Action<WsBaseConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var options = new WsBaseConfiguration();
        configure?.Invoke(options);

        var conventionBuilders = new List<IEndpointConventionBuilder>();

        var app = builder.CreateApplicationBuilder();
        app.UseWebSockets();
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
