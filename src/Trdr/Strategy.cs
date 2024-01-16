using System.Reactive.Concurrency;
using Nito.AsyncEx;
using Trdr.Reactive;

namespace Trdr;

/// <summary>
/// Defines the base class for all strategies.
/// </summary>
public abstract class Strategy
{
    private readonly SemaphoreSlim _mutex = new(1);
    private bool _running;

    private TaskScheduler MainTaskScheduler { get; set; } = null!;
    private IScheduler MainRxScheduler { get; set; } = null!;

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
                            // handlers on it.
                            MainTaskScheduler = AsyncContext.Current!.Scheduler;
                            MainRxScheduler = new TaskPoolScheduler(new TaskFactory(MainTaskScheduler));

                            var runInternalTask = RunInternal(cancellationToken);
                            startEvent.Set();
                            return runInternalTask;
                        });
                });
        }

        await startEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
        return runTask;
    }

    /// <summary>
    /// Subscribes to the given <paramref name="stream"/> with a handler for the next update (<paramref name="onNext"/>.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="onNext"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    protected Task Subscribe<T>(
        IObservable<T> stream, Action<T> onNext, CancellationToken cancellationToken)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (onNext == null) throw new ArgumentNullException(nameof(onNext));

        var handler = new SubscribeAllHandler(MainRxScheduler);
        return handler.Subscribe(stream, onNext, cancellationToken);
    }

    /// <summary>
    /// Subscribes to the given <paramref name="stream"/> with a handler for the next update (<paramref name="onNext"/>.
    /// Note, that <paramref name="onNext"/> will only receive the latest update from the stream.
    /// That is if <paramref name="onNext"/> does not complete immediately, any intermediate updates that emit during
    /// this time will be dropped.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="onNext"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    protected Task SubscribeLatest<T>(
        IObservable<T> stream, Func<T, Task> onNext, CancellationToken cancellationToken)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (onNext == null) throw new ArgumentNullException(nameof(onNext));

        var handler = new SubscribeLatestHandler(MainRxScheduler);
        return handler.Subscribe(stream, onNext, cancellationToken);
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