using BenchmarkDotNet.Attributes;
using Nito.AsyncEx;
using Trdr.Async;

namespace Trdr.Benchmarks.Async;

public class AsyncMultiAutoResetEventBenchmarks
{
    [Benchmark]
    public async Task Release_multiple_awaiters()
    {
        var @event = new AsyncMultiAutoResetEvent();

        const int tasksCount = 10000;

        var countdown = new AsyncCountdownEvent(tasksCount);
        var tasks = Enumerable.Range(1, tasksCount)
            .Select(_ => Task.Run(
                () =>
                {
                    var task = @event.Wait();
                    countdown.Signal();
                    return task;
                }))
            .ToList();

        await countdown.WaitAsync();
        @event.Set();

        await Task.WhenAll(tasks);
    }
}