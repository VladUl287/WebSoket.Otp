using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions.Connections;

namespace WebSockets.Otp.Redis.Extensions;

public static class RedisExtensions
{
    public static IServiceCollection AddConnectionServices(this IServiceCollection services)
    {
        services.AddSingleton<IWsConnectionManager, RedisWsConnectionManager>();
        return services;
    }
}
