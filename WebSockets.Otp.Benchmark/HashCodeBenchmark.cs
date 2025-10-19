using BenchmarkDotNet.Attributes;
using System.IO.Hashing;
using System.Security.Cryptography;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
public class HashCodeBenchmark
{
    private byte[] _smallData;
    private byte[] _mediumData;
    private byte[] _largeData;
    private byte[] _veryLargeData;

    [GlobalSetup]
    public void Setup()
    {
        _smallData = System.Text.Encoding.UTF8.GetBytes("Hello World");

        _mediumData = System.Text.Encoding.UTF8.GetBytes("This is a longer text that might represent a typical paragraph in an application.");

        _largeData = new byte[1024 * 10]; // 10KB
        new Random(42).NextBytes(_largeData);

        _veryLargeData = new byte[1024 * 100]; // 100KB
        new Random(42).NextBytes(_veryLargeData);
    }

    [Benchmark]
    public int GetHashCode_SmallData()
    {
        return GetHashCodeAllocFree(_smallData);
    }

    [Benchmark]
    public int GetHashCode_VeryLargeData()
    {
        return GetHashCodeAllocFree(_veryLargeData);
    }

    [Benchmark]
    public uint XXGetHashCode_SmallData()
    {
        return GetXXHash32(_smallData);
    }

    [Benchmark]
    public uint XXGetHashCode_VeryLargeData()
    {
        return GetXXHash32(_veryLargeData);
    }

    [Benchmark]
    public ulong XX64GetHashCode_SmallData()
    {
        return GetXXHash64(_smallData);
    }

    [Benchmark]
    public ulong XX64GetHashCode_VeryLargeData()
    {
        return GetXXHash64(_veryLargeData);
    }

    public static int GetHashCodeAllocFree(ReadOnlySpan<byte> utf8Bytes)
    {
        var hash = new HashCode();
        hash.AddBytes(utf8Bytes);
        return hash.ToHashCode();
    }

    public static uint GetXXHash32(ReadOnlySpan<byte> data)
    {
        return XxHash32.HashToUInt32(data);
    }

    public static ulong GetXXHash64(ReadOnlySpan<byte> data)
    {
        return XxHash64.HashToUInt64(data);
    }
}
