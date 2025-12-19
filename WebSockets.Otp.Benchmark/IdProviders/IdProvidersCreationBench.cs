using BenchmarkDotNet.Attributes;
using WebSockets.Otp.Abstractions.Contracts;

namespace WebSockets.Otp.Benchmark.IdProviders;

[MemoryDiagnoser]
public class IdProvidersCreationBench
{
    private static readonly IIdProvider GuidIdProvider = new GuidIdProvider();
    private static readonly IIdProvider UlidIdProvider = new UlidIdProvider();

    [Benchmark]
    public string GuidProvider() => GuidIdProvider.Create();

    [Benchmark]
    public string UlidProvider() => UlidIdProvider.Create();
}
