using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
public class ServiceResolve
{
    private ServiceProvider _provider = null!;
    private const string Key = "myKey";

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Regular service
        services.AddSingleton<IMyService, MyService>();

        // Keyed service (.NET 8+)
        services.AddKeyedSingleton<IMyService, MyService>(Key);

        _provider = services.BuildServiceProvider();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _provider.Dispose();
    }

    [Benchmark(Baseline = true)]
    public IMyService? GetService()
    {
        return _provider.GetService<IMyService>();
    }

    [Benchmark]
    public IMyService? GetKeyedService()
    {
        return _provider.GetKeyedService<IMyService>(Key);
    }

    [Benchmark]
    public IMyService GetRequiredService()
    {
        return _provider.GetRequiredService<IMyService>();
    }

    [Benchmark]
    public IMyService GetRequiredKeyedService()
    {
        return _provider.GetRequiredKeyedService<IMyService>(Key);
    }
}

public interface IMyService { }
public class MyService : IMyService { }
