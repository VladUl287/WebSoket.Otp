using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services.Utils;

public sealed class EndpointValidator(IServiceScopeFactory factory) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        using var scope = factory.CreateScope();

        var endpoints = scope.ServiceProvider
            .GetKeyedServices<IWsEndpoint>(KeyedService.AnyKey);

        var options = scope.ServiceProvider
            .GetRequiredService<WsConfiguration>();

        foreach (var endpoint in endpoints)
        {
            var attribute = endpoint
                .GetType()
                .GetCustomAttribute<WsEndpointAttribute>();

            Validate(attribute, options);
        }

        return next;
    }

    public static WsEndpointAttribute Validate(WsEndpointAttribute attribute, WsConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(config);

        var key = attribute.Key;

        ArgumentException.ThrowIfNullOrEmpty(key, "WsEndpoint key cannot be null or empty");

        if (attribute.Key.Length < config.Keys.MinLength)
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' is too short. Minimum length is {config.Keys.MinLength}");

        if (attribute.Key.Length > config.Keys.MaxLength)
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' is too long. Maximum length is {config.Keys.MaxLength}");

        if (config.Keys.Pattern is not null && !config.Keys.Pattern.IsMatch(attribute.Key))
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' does not match the required pattern");

        return attribute;
    }
}
