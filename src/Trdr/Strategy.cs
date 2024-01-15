using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Nito.AsyncEx;

namespace Trdr;

/// <summary>
/// Defines the base class for all strategies.
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

    protected Task Subscribe<T>(IObservable<T> stream, Action<T> handleItem, CancellationToken cancellationToken)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (handleItem == null) throw new ArgumentNullException(nameof(handleItem));

        var tcs = new TaskCompletionSource();
        stream
            .ObserveOn(new SynchronizationContextScheduler(SynchronizationContext.Current!))
            .Subscribe(
                handleItem,
                onError: ex => tcs.TrySetException(ex),
                onCompleted: () => tcs.TrySetResult());

        using var cancellation = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        return tcs.Task;
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