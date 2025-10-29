using BenchmarkDotNet.Running;
using WebSockets.Otp.Benchmark;
using WebSockets.Otp.Benchmark.Benchmarks;

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

//var tes = new StringPoolBenchmark();
//tes.Setup();

//Console.WriteLine(tes.Comunity_Strign_Pool_Existing_Key());
//Console.WriteLine(tes.Preloaded_Strign_Pool_Existing_Key());
//Console.WriteLine(tes.Preloaded_Strign_Pool_Unsafe_Existing_Key());

//BenchmarkRunner.Run<StringPoolBenchmark>();