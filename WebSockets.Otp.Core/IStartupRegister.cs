using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Core;

public interface IStartupRegister
{
    void Register(IServiceProvider sp);
}

public sealed class DefaultStartupRegistrar : IStartupRegister
{
    public void Register(IServiceProvider sp)
    {
        var reg = sp.GetRequiredService<IWsEndpointRegistry>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types = assemblies.SelectMany(a =>
        {
            try
            {
                return a.GetTypes();
            }
            catch
            {
                return [];
            }
        })
        .Where(t => !t.IsAbstract && t.GetCustomAttribute<WsEndpointAttribute>() is not null);

        foreach (var t in types) reg.Register(t);
    }
}

public static class AppServiceProviderExtensions
{
    public static void InitializeWs(this IServiceProvider sp)
    {
        var registrar = sp.GetService<IStartupRegister>();
        registrar?.Register(sp);
    }
}
