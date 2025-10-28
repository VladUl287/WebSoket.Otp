using BenchmarkDotNet.Running;
using System.Reflection;
using WebSockets.Otp.Benchmark;

//Console.WriteLine(HashCodeBenchmark.GetXXHash32(System.Text.Encoding.UTF8.GetBytes("Hello World")));
//Console.WriteLine(HashCodeBenchmark.GetHashCodeAllocFree(System.Text.Encoding.UTF8.GetBytes("Hello World")));
//Console.WriteLine(HashCodeBenchmark.GetXXHash64(System.Text.Encoding.UTF8.GetBytes("Hello World")));

//var benc = new SequentialMessageProcessorBenchmark();
//benc.Setup();
//benc.IterationSetup();

//benc.BenchmarkMethod();

//benc.Cleanup();

//var ven = new MethodInvokeBenchmark();
//ven.Setup();
//ven.Delegate();

//BenchmarkRunner.Run<MethodInvokeBenchmark>();

BenchmarkRunner.Run<SequentialMessageProcessorBenchmark>();