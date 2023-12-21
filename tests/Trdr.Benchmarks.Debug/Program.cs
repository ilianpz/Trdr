using BenchmarkDotNet.Running;
using Trdr.Benchmarks.Async;

// Use Run(new DebugInProcessConfig()) to debug
BenchmarkRunner.Run<AsyncMultiAutoResetEventBenchmarks>();