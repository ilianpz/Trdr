using BenchmarkDotNet.Running;

// Use Run(new DebugInProcessConfig()) to debug
BenchmarkRunner.Run<Dummy>();

class Dummy { }