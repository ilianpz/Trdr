using Nito.AsyncEx;
using Trdr.Async;
using TaskExtensions = Trdr.Async.TaskExtensions;

namespace Trdr.Tests;

public sealed class TaskExtensionsTests
{
    [Test]
    public void Run_can_schedule_correctly()
    {
        var schedulerPair = new ConcurrentExclusiveSchedulerPair();
        var secondTaskRanEvent = new AsyncAutoResetEvent();

        TaskExtensions.Run(BlockingTask, schedulerPair.ExclusiveScheduler).Forget();
        TaskExtensions.Run(SecondTask, schedulerPair.ExclusiveScheduler).Forget();

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));
        Assert.That(
            async () => await secondTaskRanEvent.WaitAsync(cts.Token),
            Throws.InstanceOf<OperationCanceledException>());

        return;

        async Task BlockingTask()
        {
            await Task.Yield();
            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        async Task SecondTask()
        {
            await Task.Yield();
            secondTaskRanEvent.Set();
        }
    }
}