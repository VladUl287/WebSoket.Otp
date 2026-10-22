using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using WebSockets.Otp.Abstractions.Serializers;

namespace WebSockets.Otp.MessagePack.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessagePackSerializer(this IServiceCollection services)
    {
        var options = MessagePackSerializerOptions.Standard;
        return services.AddMessagePackSerializer(options);
    }

    public static IServiceCollection AddMessagePackSerializer(this IServiceCollection services, Action<MessagePackSerializerOptions> configure)
    {
        var options = MessagePackSerializerOptions.Standard;
        configure(options);
        return services.AddMessagePackSerializer(options);
    }

    public static IServiceCollection AddMessagePackSerializer(this IServiceCollection services, MessagePackSerializerOptions options)
    {
        return services.AddSingleton<IMessageSerializer>(new MessagePackMessageSerializer(options));
    }
}
