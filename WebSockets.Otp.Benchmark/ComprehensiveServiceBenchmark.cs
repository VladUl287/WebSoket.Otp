using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
public class ComprehensiveServiceBenchmark
{
    private IServiceProvider _serviceProvider;
    private ConcurrentDictionary<string, Type> _concurrentDictionary;
    private FrozenDictionary<string, Type> _frozenDictionary;

    private readonly string[] _keys = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
    private int _requestCount = 0;

    [GlobalSetup]
    public void Setup()
    {
        // Setup DI container with keyed services
        var services = new ServiceCollection();

        // Add more services
        services.AddKeyedSingleton<IService, ServiceA>("A");
        services.AddKeyedSingleton<IService, ServiceB>("B");
        services.AddKeyedSingleton<IService, ServiceC>("C");
        services.AddKeyedSingleton<IService, ServiceA>("D");
        services.AddKeyedSingleton<IService, ServiceB>("E");
        services.AddKeyedSingleton<IService, ServiceC>("F");
        services.AddKeyedSingleton<IService, ServiceA>("G");
        services.AddKeyedSingleton<IService, ServiceB>("H");
        services.AddKeyedSingleton<IService, ServiceC>("I");
        services.AddKeyedSingleton<IService, ServiceA>("J");

        _serviceProvider = services.BuildServiceProvider();

        // Setup all dictionary types
        var initialDict = new Dictionary<string, Type>
        {
            ["A"] = typeof(ServiceA),
            ["B"] = typeof(ServiceB),
            ["C"] = typeof(ServiceC),
            ["D"] = typeof(ServiceA),
            ["E"] = typeof(ServiceB),
            ["F"] = typeof(ServiceC),
            ["G"] = typeof(ServiceA),
            ["H"] = typeof(ServiceB),
            ["I"] = typeof(ServiceC),
            ["J"] = typeof(ServiceA)
        };


        _concurrentDictionary = new ConcurrentDictionary<string, Type>(initialDict);
        _frozenDictionary = initialDict.ToFrozenDictionary();
    }

    [Benchmark(Baseline = true)]
    public IService KeyedService_Resolution()
    {
        var key = _keys[_requestCount++ % _keys.Length];
        return _serviceProvider.GetKeyedService<IService>(key);
    }

    //[Benchmark]
    //public IService ConcurrentDictionary_ThenResolve()
    //{
    //    var key = _keys[_requestCount++ % _keys.Length];
    //    if (_concurrentDictionary.TryGetValue(key, out var type))
    //    {
    //        return _serviceProvider.GetService(type) as IService;
    //    }
    //    return null;
    //}

    [Benchmark]
    public IService FrozenDictionary_ThenResolve()
    {
        var key = _keys[_requestCount++ % _keys.Length];
        if (_frozenDictionary.TryGetValue(key, out var type))
        {
            return _serviceProvider.GetService(type) as IService;
        }
        return null;
    }

    [Benchmark]
    public string KeyedService_WithExecution()
    {
        var key = _keys[_requestCount++ % _keys.Length];
        var service = _serviceProvider.GetKeyedService<IService>(key);
        return service?.Execute();
    }

    [Benchmark]
    public string FrozenDictionary_WithExecution()
    {
        var key = _keys[_requestCount++ % _keys.Length];
        if (_frozenDictionary.TryGetValue(key, out var type))
        {
            var service = _serviceProvider.GetService(type) as IService;
            return service?.Execute();
        }
        return null;
    }

    //[Benchmark]
    //public string ConcurentDictionary_WithExecution()
    //{
    //    var key = _keys[_requestCount++ % _keys.Length];
    //    if (_concurrentDictionary.TryGetValue(key, out var type))
    //    {
    //        var service = _serviceProvider.GetService(type) as IService;
    //        return service?.Execute();
    //    }
    //    return null;
    //}

    //[Benchmark]
    //public Type FrozenDictionary_PureLookup()
    //{
    //    _requestCount = (_requestCount + 1) % _keys.Length;
    //    var key = _keys[_requestCount];
    //    _frozenDictionary.TryGetValue(key, out var type);
    //    return type;
    //}

    //[Benchmark]
    //public Type ConcurrentDictionary_PureLookup()
    //{
    //    _requestCount = (_requestCount + 1) % _keys.Length;
    //    var key = _keys[_requestCount];
    //    _concurrentDictionary.TryGetValue(key, out var type);
    //    return type;
    //}
}

public interface IService
{
    string Execute();
}

public class ServiceA : IService
{
    public string Execute() => "ServiceA";
}

public class ServiceB : IService
{
    public string Execute() => "ServiceB";
}

public class ServiceC : IService
{
    public string Execute() => "ServiceC";
}
