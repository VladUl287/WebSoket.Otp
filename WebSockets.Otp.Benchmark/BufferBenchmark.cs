using BenchmarkDotNet.Attributes;
using Microsoft.IO;
using WebSockets.Otp.Core.Helpers;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
public class WriteBenchmarks
{
    private static readonly byte[] Data = Enumerable.Range(0, 255).Select(c => (byte)c).ToArray();
    private const int WriteIterations = 50;
    private const int InitialCapacity = 8096;

    private static readonly RecyclableMemoryStreamManager Manager = new();

    [Benchmark]
    public long CreateNew_MemoryStream()
    {
        using var stream = new MemoryStream(InitialCapacity);
        for (int i = 0; i < WriteIterations; i++)
        {
            stream.Write(Data);
        }
        return stream.Length;
    }

    [Benchmark]
    public long CreateNew_RecyclableMemoryStream()
    {
        using var stream = Manager.GetStream();
        for (int i = 0; i < WriteIterations; i++)
        {
            stream.Write(Data);
        }
        return stream.Length;
    }

    [Benchmark]
    public int CreateNew_NativeChunkedBuffer()
    {
        using var stream = new NativeChunkedBuffer(InitialCapacity);
        for (int i = 0; i < WriteIterations; i++)
        {
            stream.Write(Data);
        }
        return stream.Length;
    }
}

[MemoryDiagnoser]
public class ResetBenchmarks
{
    private static readonly byte[] Data = [.. Enumerable.Range(0, 255).Select(c => (byte)c)];
    private const int WriteIterations = 50;

    [GlobalSetup]
    public void IterationSetup()
    {
        MemoryStream = new MemoryStream(8096);
        RecyclableStream = Manager.GetStream();
        NativeBuffer = new NativeChunkedBuffer(8096);
    }

    [GlobalCleanup]
    public void IterationCleanup()
    {
        MemoryStream?.Dispose();
        RecyclableStream?.Dispose();
        NativeBuffer?.Dispose();
    }

    private MemoryStream MemoryStream;
    private RecyclableMemoryStream RecyclableStream;
    private NativeChunkedBuffer NativeBuffer;
    private static readonly RecyclableMemoryStreamManager Manager = new();

    [Benchmark]
    public long Reset_MemoryStream()
    {
        for (int i = 0; i < WriteIterations; i++)
        {
            MemoryStream.Write(Data);
        }
        var length = MemoryStream.Length;
        MemoryStream.SetLength(0);
        return length;
    }

    [Benchmark]
    public long Reset_RecyclableMemoryStream()
    {
        for (int i = 0; i < WriteIterations; i++)
        {
            RecyclableStream.Write(Data);
        }
        var length = RecyclableStream.Length;
        RecyclableStream.SetLength(0);
        return length;
    }

    [Benchmark]
    public long Reset_NativeChunkedBuffer()
    {
        for (int i = 0; i < WriteIterations; i++)
        {
            NativeBuffer.Write(Data);
        }
        var length = NativeBuffer.Length;
        NativeBuffer.SetLength(0);
        return length;
    }
}

[MemoryDiagnoser]
public class ShrinkBenchmarks
{
    private static readonly byte[] Data = [.. Enumerable.Range(0, 255).Select(c => (byte)c)];
    private const int WriteIterations = 50;

    private MemoryStream MemoryStream;
    private RecyclableMemoryStream RecyclableStream;
    private NativeChunkedBuffer NativeBuffer;
    private static readonly RecyclableMemoryStreamManager Manager = new();

    [GlobalSetup]
    public void IterationSetup()
    {
        MemoryStream = new MemoryStream(8096);
        RecyclableStream = Manager.GetStream();
        NativeBuffer = new NativeChunkedBuffer(8096);
    }

    [GlobalCleanup]
    public void IterationCleanup()
    {
        MemoryStream?.Dispose();
        RecyclableStream?.Dispose();
        NativeBuffer?.Dispose();
    }

    [Benchmark]
    public int Shrink_MemoryStreamBuffer()
    {
        for (int i = 0; i < WriteIterations; i++)
        {
            MemoryStream.Write(Data, 0, Data.Length);
        }
        var result = (int)MemoryStream.Length;
        MemoryStream.SetLength(0);
        MemoryStream.Capacity = 8096;
        return result;
    }


    [Benchmark]
    public int Shrink_RecycableMemoryStreamBuffer()
    {
        for (int i = 0; i < WriteIterations; i++)
        {
            RecyclableStream.Write(Data, 0, Data.Length);
        }
        var result = (int)RecyclableStream.Length;
        RecyclableStream.SetLength(0);
        RecyclableStream.Capacity = 8096;
        return result;
    }

    [Benchmark]
    public int Shrink_NativeChunkStreamBuffer()
    {
        for (int i = 0; i < WriteIterations; i++)
        {
            NativeBuffer.Write(Data);
        }
        var result = NativeBuffer.Length;
        NativeBuffer.SetLength(0);
        NativeBuffer.Shrink();
        return result;
    }
}