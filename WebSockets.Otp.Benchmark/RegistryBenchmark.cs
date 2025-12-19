using BenchmarkDotNet.Attributes;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
public class RegistryBenchmark
{
    public string Value1 = new('c', 500);
    public string Value2;
    public string Value3 = new('c', 500);

    public RegistryBenchmark() => Value2 = Value1;

    [Benchmark]
    public bool ValuesEquals() => Equals(Value1, Value3);

    [Benchmark]
    public bool ReferencesEquals() => Equals(Value1, Value2);

    [Benchmark]
    public bool ValuesEqualsIn() => EqualsIn(Value1, Value3);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Equals(string methodA, string methodB) =>
        object.ReferenceEquals(methodA, methodB) || StringComparer.OrdinalIgnoreCase.Equals(methodA, methodB);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool EqualsIn(in string methodA, in string methodB) =>
        object.ReferenceEquals(methodA, methodB) || StringComparer.OrdinalIgnoreCase.Equals(methodA, methodB);
}

[MemoryDiagnoser]
public class RegistryDictionaryBenchmark
{
    private FrozenDictionary<string, Type> _store;
    private static readonly string _reference = new('c', 50);
    private static readonly string _newReference = new('c', 50);

    public RegistryDictionaryBenchmark()
    {
        var dictionary = new Dictionary<string, Type>();

        foreach (var i in Enumerable.Range(0, 100).Select(c => c.ToString()))
            dictionary.Add(i, typeof(string));

        dictionary.Add(_reference, typeof(string));

        _store = dictionary.ToFrozenDictionary();
    }

    [Benchmark]
    public Type? GetDefaultStore()
    {
        _store.TryGetValue(_reference, out var type);
        return type;
    }

    [Benchmark]
    public Type? GetDefaultStore_NewReference()
    {
        _store.TryGetValue(_newReference, out var type);
        return type;
    }
}
