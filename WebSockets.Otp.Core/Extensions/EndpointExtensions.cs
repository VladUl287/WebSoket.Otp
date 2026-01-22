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
    public static WsEndpointConventionBuilder MapEndpoints(
        this IEndpointRouteBuilder builder, string pattern, Action<WsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var options = GetOptions(builder, configure);

        var app = builder.CreateApplicationBuilder();
        app.UseWebSockets(options.WebSocketOptions);
        app.Run((context) =>
        {
            return context.RequestServices
                .GetRequiredService<IConnectionHandler>()
                .HandleAsync(context, options);
        });
        var executeHandler = app.Build();

        var executeBuilder = builder
            .Map(pattern, executeHandler)
            .DisableRequestTimeout()
            .WithMetadata(options);

        executeBuilder.Add(builder =>
        {
            foreach (var data in options.AuthorizationData)
                builder.Metadata.Add(data);
        });

        return new WsEndpointConventionBuilder(executeBuilder);
    }

    private static WsOptions GetOptions(IEndpointRouteBuilder builder, Action<WsOptions>? configure)
    {
        if (configure is null)
            return builder.ServiceProvider.GetRequiredService<WsGlobalOptions>();

        var options = new WsOptions();
        configure(options);
        return options;
    }
}
