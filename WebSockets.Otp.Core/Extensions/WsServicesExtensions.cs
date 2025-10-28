using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.IdProviders;

namespace WebSockets.Otp.Core.Extensions;

public static class WsServicesExtensions
{
    public static IServiceCollection AddUlidIdProvider(this IServiceCollection services)
    {
        services.AddSingleton<IIdProvider, UlidIdProvider>();
        return services;
    }
}
