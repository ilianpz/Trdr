using Nito.AsyncEx;
using Trdr.Async;
using Trdr.Reactive;
using TaskExtensions = Trdr.Async.TaskExtensions;

namespace Trdr;

public sealed class Sentinel<T> : IDisposable
{
    private readonly IObservable<T> _observable;
    private readonly AsyncProducerConsumerQueue<T> _queue = new();
    private readonly AsyncMultiAutoResetEvent _signal = new();

    private readonly Action<T> _handler;
    private readonly TaskScheduler _scheduler;
    private readonly CancellationTokenSource _disposeCts = new();

    private bool _started;
    private IDisposable? _subscription;

    private Sentinel(IObservable<T> observable, Action<T> handler, TaskScheduler scheduler)
    {
        _observable = observable ?? throw new ArgumentNullException(nameof(observable));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

        // We need to publish items on the given scheduler which should be the strategy's main thread.
        TaskExtensions.Run(Publish, _scheduler).Forget();
    }

    internal static Sentinel<TNew> Create<TNew>(IAsyncEnumerable<TNew> enumerable, Action<TNew> handler, TaskScheduler taskScheduler)
    {
        return Create(enumerable.ToObservable(), handler, taskScheduler);
    }

    internal static Sentinel<TNew> Create<TNew>(IObservable<TNew> observable, Action<TNew> handler, TaskScheduler taskScheduler)
    {
        return new Sentinel<TNew>(observable, handler, taskScheduler);
    }

    public Sentinel<(T, TOther)> Combine<TOther>(IAsyncEnumerable<TOther> enumerable, Action<TOther> handler)
    {
        return Combine(enumerable.ToObservable(), handler);
    }

    public Sentinel<(T, TOther)> Combine<TOther>(IObservable<TOther> observable, Action<TOther> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        VerifyNotStarted();

        return new Sentinel<(T, TOther)>(_observable.ZipWithLatest(observable), Handle, _scheduler);

        // We need to wrap the original handler because the generic type changes
        // with the new Event instance.
        void Handle((T, TOther) item)
        {
            var newHandler = handler;

            _handler(item.Item1);
            newHandler(item.Item2);
        }
    }

    public void Start()
    {
        VerifyNotStarted();

        _subscription = _observable.Subscribe(item => _queue.Enqueue(item));
        _started = true;
    }

    public async Task Watch(Func<bool> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        while (true)
        {
            await Wait(cancellationToken);
            if (predicate())
                return;
        }
    }

    public Task Wait(CancellationToken cancellationToken = default)
    {
       return _signal.WaitAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_subscription == null) return;

        _subscription.Dispose();
        _subscription = null;

        _disposeCts.Cancel();
        _disposeCts.Dispose();
    }

    private void VerifyNotStarted()
    {
        if (_started)
            throw new InvalidOperationException("Already started.");
    }

    private async Task Publish()
    {
        try
        {
            while (true)
            {
                var item = await _queue.DequeueAsync(_disposeCts.Token);

                try
                {
                    _handler(item);
                    _signal.Set();
                }
                catch { /* NOP */ }
            }
        }
        catch (OperationCanceledException) { }
    }
}