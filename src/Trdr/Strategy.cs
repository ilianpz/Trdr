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

    protected Sentinel<T> Subscribe<T>(IAsyncEnumerable<T> enumerable, Action<Timestamped<T>> handler)
    {
        if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));
        return Subscribe(enumerable.ToObservable(), handler);
    }

    protected Sentinel<T> Subscribe<T>(IObservable<T> observable, Action<Timestamped<T>> handler)
    {
        if (observable == null) throw new ArgumentNullException(nameof(observable));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var sentinel = new Sentinel<T>(observable, handler, TaskScheduler);
        sentinel.Start();
        return sentinel;
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