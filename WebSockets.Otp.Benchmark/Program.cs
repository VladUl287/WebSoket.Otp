using BenchmarkDotNet.Running;
using WebSockets.Otp.Benchmark;

//Console.WriteLine(HashCodeBenchmark.GetXXHash32(System.Text.Encoding.UTF8.GetBytes("Hello World")));
//Console.WriteLine(HashCodeBenchmark.GetHashCodeAllocFree(System.Text.Encoding.UTF8.GetBytes("Hello World")));
//Console.WriteLine(HashCodeBenchmark.GetXXHash64(System.Text.Encoding.UTF8.GetBytes("Hello World")));

BenchmarkRunner.Run<ExtractFieldBenchmark>();