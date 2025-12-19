using BenchmarkDotNet.Attributes;
using WebSockets.Otp.Abstractions.Contracts;
using WebSockets.Otp.Core.Services.IdProviders;

namespace WebSockets.Otp.Benchmark.IdProviders;

[MemoryDiagnoser]
public class IdProvidersCreationBench
{
    private static readonly IIdProvider GuidIdProvider = new GuidIdProvider();
    private static readonly IIdProvider UlidIdProvider = new UlidIdProvider();

    public static IIdProvider GuidIdProvider1 => GuidIdProvider;

    [Benchmark]
    public string GuidProvider() => GuidIdProvider1.Create();

    [Benchmark]
    public string UlidProvider() => UlidIdProvider.Create();
}
