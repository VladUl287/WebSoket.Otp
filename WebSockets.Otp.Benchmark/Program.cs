using BenchmarkDotNet.Running;
using WebSockets.Otp.Benchmark;

//var benc = new SequentialMessageProcessorBenchmark();
//benc.Setup();
//benc.IterationSetup();

//benc.BenchmarkMethod();

//benc.Cleanup();

//var ven = new MethodInvokeBenchmark();
//ven.Setup();
//ven.Delegate();

//BenchmarkRunner.Run<MethodInvokeBenchmark>();

//BenchmarkRunner.Run<SequentialMessageProcessorBenchmark>();

//BenchmarkRunner.Run<IdProvidersCreationBench>();

BenchmarkRunner.Run<ExtractFieldBenchmark>();