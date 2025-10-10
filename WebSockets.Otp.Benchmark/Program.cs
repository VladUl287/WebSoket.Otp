using BenchmarkDotNet.Running;
using WebSockets.Otp.Benchmark;

BenchmarkRunner.Run<WriteBenchmarks>();
BenchmarkRunner.Run<ResetBenchmarks>();
BenchmarkRunner.Run<ShrinkBenchmarks>();