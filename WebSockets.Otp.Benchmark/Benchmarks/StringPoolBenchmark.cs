using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance.Buffers;
using System.Text;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Benchmark.Benchmarks;

[MemoryDiagnoser]
public class StringPoolBenchmark
{
    private StringPool stringPool;
    private PreloadedStringPool preloadedStringPool;
    private PreloadedStringPool preloadedStringPoolUnsafe;

    private readonly Encoding encoding = Encoding.UTF8;

    private static readonly string key = "longlonglonglonglonglonglonglonglonglonglonglonglonglonglonglonglonglonglonglonglonglonglonglongkey";
    private static readonly string not_key = "longlonglonglonglonglonglonglonglonglonglonglonglonglonglonglonglonglongkey34534556745634657764567456574765471";
    private readonly ReadOnlyMemory<char> keyChars = key.AsMemory();
    private readonly ReadOnlyMemory<char> not_keyChars = not_key.AsMemory();
    private readonly ReadOnlyMemory<byte> keyBytes = Encoding.UTF8.GetBytes(key);
    private readonly ReadOnlyMemory<byte> not_keyBytes = Encoding.UTF8.GetBytes(not_key);

    [GlobalSetup]
    public void Setup()
    {
        var listStrings = GenerateRandomStrings(100, 50).ToList();
        listStrings.Insert(51, key);

        stringPool = new StringPool();
        preloadedStringPool = new(listStrings, encoding);
        preloadedStringPoolUnsafe = new(listStrings, encoding, true);
        foreach (var item in listStrings)
            stringPool.Add(item);
    }

    [Benchmark]
    public string Comunity_Strign_Pool_Existing_Key()
    {
        if (stringPool.TryGet(keyChars.Span, out var value))
            return value;
        return new string(keyChars.Span);
    }

    [Benchmark]
    public string Comunity_Strign_Pool_Get_Or_Add_Existing_Key()
    {
        return stringPool.GetOrAdd(keyBytes.Span, encoding);
    }

    [Benchmark]
    public string Preloaded_Strign_Pool_Existing_Key()
    {
        return preloadedStringPool.Intern(keyBytes.Span);
    }

    [Benchmark]
    public string Preloaded_Strign_Pool_Unsafe_Existing_Key()
    {
        return preloadedStringPoolUnsafe.Intern(keyBytes.Span);
    }

    [Benchmark]
    public string Comunity_Strign_Pool_Not_Existing_Key()
    {
        if (stringPool.TryGet(not_keyChars.Span, out var value))
            return value;
        return new string(not_keyChars.Span);
    }

    [Benchmark]
    public string Preloaded_Strign_Pool_Not_Existing_Key()
    {
        return preloadedStringPool.Intern(not_keyBytes.Span);
    }

    [Benchmark]
    public string Preloaded_Strign_Pool_Unsafe_Not_Existing_Key()
    {
        return preloadedStringPoolUnsafe.Intern(not_keyBytes.Span);
    }

    private static readonly Random _random = new();
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string[] GenerateRandomStrings(int count, int length = 10)
    {
        var strings = new string[count];

        for (int i = 0; i < count; i++)
        {
            strings[i] = new string(Enumerable.Repeat(Chars, length)
                .Select(s => s[_random.Next(s.Length)])
                .ToArray());
        }

        return strings;
    }
}
