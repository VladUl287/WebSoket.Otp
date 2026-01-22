using BenchmarkDotNet.Running;
using WebSockets.Otp.Benchmark;

//var bench = new EndpointInvokerBench();

//bench.Setup();

//await bench.Invoke_Reflection_Factory();

//bench.Cleanup();

BenchmarkRunner.Run<EndpointInvokerBench>();
