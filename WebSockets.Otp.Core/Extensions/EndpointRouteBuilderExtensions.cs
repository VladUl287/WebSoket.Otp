using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Abstractions.Options;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static WsEndpointConventionBuilder MapEndpoints(
        this IEndpointRouteBuilder builder, string pattern, Action<WsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(pattern, nameof(pattern));

        var options = EnsureOptions(builder, configure);

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

    private static WsOptionsSnapshot EnsureOptions(IEndpointRouteBuilder builder, Action<WsOptions>? configure)
    {
        var options = builder.ServiceProvider.GetService<WsOptions>() ?? new WsOptions();
        configure?.Invoke(options);

        var authPipeline = builder.CreateApplicationBuilder();
        authPipeline.UseAuthentication();
        authPipeline.UseAuthorization();
        authPipeline.Run(ctx => Task.CompletedTask);

        return new WsOptionsSnapshot(options)
        {
            AuthPipeline = authPipeline.Build()
        };
    }
}
