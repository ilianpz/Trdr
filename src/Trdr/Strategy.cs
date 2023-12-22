using Nito.AsyncEx;
using Trdr.Reactive;

namespace Trdr;

/// <summary>
/// Defines the base class for all strategies. The strategy will run on a single main thread just
/// like UI applications.
/// </summary>
public abstract class Strategy
{
    private bool _running;

    private TaskScheduler TaskScheduler { get; set; } = null!;

    public async Task<Task> Start(CancellationToken cancellationToken = default)
    {
        var startEvent = new AsyncManualResetEvent(false);

        Task runTask;
        using (var thread = new AsyncContextThread())
        {
            // ReSharper disable once MethodSupportsCancellation
            runTask = thread.Factory.StartNew(
                () =>
                {
                    AsyncContext.Run(
                        () =>
                        {
                            //
                            // Run the strategy in a single main thread just like UI apps.
                            //

                            // Grab the TaskScheduler for this single thread so we can
                            // schedule Sentinel actions on it.
                            TaskScheduler = AsyncContext.Current!.Scheduler;

                            var runInternalTask = RunInternal(cancellationToken);
                            startEvent.Set();
                            return runInternalTask;
                        });
                });
        }

        await startEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
        return runTask;
    }

    protected Sentinel<T> CreateSentinel<T>(IAsyncEnumerable<T> enumerable, Action<Timestamped<T>> handler)
    {
        return Sentinel.Create(enumerable, handler, TaskScheduler);
    }

    private Task RunInternal(CancellationToken cancellationToken)
    {
        if (_running)
            throw new InvalidOperationException("Already running.");

        _running = true;
        return Run(cancellationToken);
    }

    protected abstract Task Run(CancellationToken cancellationToken);
}