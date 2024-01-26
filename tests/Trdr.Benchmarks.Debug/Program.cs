using BenchmarkDotNet.Running;
using Trdr.Benchmarks;

// Use Run(new DebugInProcessConfig()) to debug
BenchmarkRunner.Run<NumericExtensionsBenchmarks>();