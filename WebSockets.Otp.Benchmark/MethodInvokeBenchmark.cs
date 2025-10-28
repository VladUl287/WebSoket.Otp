using BenchmarkDotNet.Attributes;
using System.Reflection;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
public class MethodInvokeBenchmark
{
    public MethodInfo DoSomethingMethod;
    public Func<ServiceBase, int, object?> DoSomethingDelegate;

    [GlobalSetup]
    public void Setup()
    {
        DoSomethingMethod = typeof(ServiceBase).GetMethod(
            nameof(Service.DoSomething),
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            [typeof(int)],
            null)!;
        DoSomethingDelegate = DoSomethingMethod.CreateDelegate<Func<ServiceBase, int, object?>>();
    }

    private static readonly Service service = new();

    [Benchmark]
    public object? Invoke()
    {
        return DoSomethingMethod.Invoke(service, [234]);
    }

    [Benchmark]
    public object? Delegate()
    {
        return DoSomethingDelegate(service, 234);
    }

    [Benchmark]
    public object? Delegate_DynamicInvoke()
    {
        return DoSomethingDelegate.DynamicInvoke(service, 234);
    }
}

public abstract class ServiceBase
{
    public abstract object DoSomething(int a);
}

public sealed class Service : ServiceBase
{
    public override object DoSomething(int a) => a + 1;
}