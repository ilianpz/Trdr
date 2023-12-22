using System.Reactive.Linq;
using Nito.AsyncEx;
using Trdr.Async;
using Trdr.Reactive;
using TaskExtensions = Trdr.Async.TaskExtensions;

namespace Trdr;

public static class Sentinel
{
    public static Sentinel<T> Create<T>(IAsyncEnumerable<T> enumerable, Action<Timestamped<T>> handler, TaskScheduler taskScheduler)
    {
        return Create(enumerable.ToObservable(), handler, taskScheduler);
    }

    public static Sentinel<T> Create<T>(IObservable<T> observable, Action<Timestamped<T>> handler, TaskScheduler taskScheduler)
    {
        return new Sentinel<T>(observable, handler, taskScheduler);
    }
}

public sealed class Sentinel<T> : IDisposable
{
    private readonly IObservable<Timestamped<T>> _observable;
    private readonly AsyncProducerConsumerQueue<Timestamped<T>> _queue = new();
    private readonly AsyncMultiAutoResetEvent _signal = new();

    private readonly Action<Timestamped<T>> _handler;
    private readonly TaskScheduler _scheduler;
    private readonly CancellationTokenSource _disposeCts = new();

    private bool _started;
    private IDisposable? _subscription;

    internal Sentinel(IObservable<T> observable, Action<Timestamped<T>> handler, TaskScheduler scheduler)
    {
        if (observable == null) throw new ArgumentNullException(nameof(observable));
        _observable = observable.Select(Timestamped.Timestamp);
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

        // We need to publish items on the given scheduler which should be the strategy's main thread.
        TaskExtensions.Run(Publish, _scheduler).Forget();
    }

    public Sentinel<(Timestamped<T>, TOther)> Combine<TOther>(IAsyncEnumerable<TOther> enumerable, Action<Timestamped<TOther>> handler)
    {
        return Combine(enumerable.ToObservable(), handler);
    }

    public Sentinel<(Timestamped<T>, TOther)> Combine<TOther>(IObservable<TOther> observable, Action<Timestamped<TOther>> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        VerifyNotStarted();

        return new Sentinel<(Timestamped<T>, TOther)>(_observable.ZipWithLatest(observable), Handle, _scheduler);

        // We need to wrap the original handler because the generic type changes
        // with the new Event instance.
        void Handle(Timestamped<(Timestamped<T>, TOther)> wrappedItem)
        {
            var newHandler = handler;

            var item = wrappedItem.Value;
            _handler(item.Item1);
            newHandler(Timestamped.Create(wrappedItem.Timestamp, item.Item2));
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