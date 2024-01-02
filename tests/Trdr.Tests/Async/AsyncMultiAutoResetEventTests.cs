using System.Collections;
using Nito.AsyncEx;
using Trdr.Async;

namespace Trdr.Tests.Async;

public sealed class AsyncMultiAutoResetEventTests
{
    [Test]
    [Timeout(5000)]
    public Task Set_stays_until_awaited()
    {
        var @event = new AsyncMultiAutoResetEvent();
        @event.Set();
        return @event.Wait();
    }

    [Test]
    [Timeout(5000)]
    public async Task Set_resets_after_await()
    {
        var @event = new AsyncMultiAutoResetEvent();
        @event.Set();
        await @event.Wait();

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));
        Assert.That(
            () => @event.Wait(cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    [Repeat(1000)]
    [Timeout(5000)]
    [TestCaseSource(nameof(TaskSchedulerCases))]
    public async Task Set_can_release_multiple_awaiters(TaskScheduler taskScheduler)
    {
        var scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
        var @event = new AsyncMultiAutoResetEvent();

        const int tasksCount = 1000;

        var countdown = new AsyncCountdownEvent(tasksCount);
        var tasks = Enumerable.Range(1, tasksCount)
            .Select(_ =>
                TaskEx.Run(
                    () =>
                    {
                        var waitTask = @event.Wait();
                        countdown.Signal();
                        return waitTask;
                    }, scheduler))
            .ToList();

        await countdown.WaitAsync();
        @event.Set();

        await Task.WhenAll(tasks);
    }

    [Test]
    [Timeout(5000)]
    public async Task Set_resets_after_multiple_awaits()
    {
        var @event = new AsyncMultiAutoResetEvent();

        const int count = 1000;

        var countdown = new AsyncCountdownEvent(count);
        var tasks = Enumerable.Range(1, count)
            .Select(_ => Task.Run(
                () =>
                {
                    var waitTask = @event.Wait();
                    countdown.Signal();
                    return waitTask;
                }))
            .ToList();

        await countdown.WaitAsync();
        @event.Set();

        await Task.WhenAll(tasks);

        // Check that the event has reset
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));
        Assert.That(
            () => @event.Wait(cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    public static IEnumerable TaskSchedulerCases
    {
        get
        {
            yield return new TestCaseData(TaskScheduler.Default)
                .SetName(nameof(TaskScheduler.Default));

            // Verifies that it still works on a single thread
            var schedulerPair = new ConcurrentExclusiveSchedulerPair();
            yield return new TestCaseData(schedulerPair.ExclusiveScheduler)
                .SetName(nameof(schedulerPair.ExclusiveScheduler));
        }
    }
}