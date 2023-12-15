// Use Run(new DebugInProcessConfig()) to debug

using BenchmarkDotNet.Running;
using Trdr.Benchmarks.Async;

BenchmarkRunner.Run<AsyncMultiAutoResetEventBenchmarks>();