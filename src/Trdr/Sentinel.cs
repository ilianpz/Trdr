using System.Reactive.Linq;
using Nito.AsyncEx;
using Trdr.Async;
using Trdr.Reactive;
using TaskExtensions = Trdr.Async.TaskExtensions;

namespace Trdr;

public abstract class Sentinel
{
    public static Sentinel Create<T>(IAsyncEnumerable<T> enumerable, Action<Timestamped<T>> handler, TaskScheduler taskScheduler)
    {
        return Create(enumerable.ToObservable(), handler, taskScheduler);
    }

    public static Sentinel Create<T>(IObservable<T> observable, Action<Timestamped<T>> handler, TaskScheduler taskScheduler)
    {
        return new Sentinel<T>(observable, handler, taskScheduler);
    }

    public abstract Sentinel Combine<TOther>(IAsyncEnumerable<TOther> enumerable, Action<Timestamped<TOther>> handler);

    public abstract Sentinel Combine<TOther>(IObservable<TOther> observable, Action<Timestamped<TOther>> handler);

    public abstract void Start();

    public abstract Task Wait(CancellationToken cancellationToken = default);

    public abstract Task Watch(Func<bool> predicate, CancellationToken cancellationToken = default);
}

public class Sentinel<T> : Sentinel, IDisposable
{
    private readonly IObservable<Timestamped<T>> _observable;
    private readonly CancellationTokenSource _disposeCts = new();

    private bool _started;
    private IDisposable? _subscription;

    internal Sentinel(IObservable<T> observable, Action<Timestamped<T>> handler, TaskScheduler scheduler)
    {
        if (observable == null) throw new ArgumentNullException(nameof(observable));
        _observable = observable.Select(Timestamped.Timestamp);
        ItemHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        PublishScheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    protected AsyncProducerConsumerQueue<Timestamped<T>> ItemsQueue { get; } = new();

    protected Action<Timestamped<T>> ItemHandler { get; }

    protected AsyncMultiAutoResetEvent ItemReceivedEvent { get; } = new(false);

    protected TaskScheduler PublishScheduler { get; }


    public override Sentinel Combine<TOther>(IAsyncEnumerable<TOther> enumerable, Action<Timestamped<TOther>> handler)
    {
        return Combine(enumerable.ToObservable(), handler);
    }

    public override Sentinel Combine<TOther>(IObservable<TOther> observable, Action<Timestamped<TOther>> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        VerifyNotStarted();

        return new Sentinel<(Timestamped<T>, TOther)>(_observable.ZipWithLatest(observable), Handle, PublishScheduler);

        // We need to wrap the original handler because the generic type changes
        // with the new Event instance.
        void Handle(Timestamped<(Timestamped<T>, TOther)> wrappedItem)
        {
            var newHandler = handler;

            var item = wrappedItem.Value;
            ItemHandler(item.Item1);
            newHandler(Timestamped.Create(wrappedItem.Timestamp, item.Item2));
        }
    }

    public override void Start()
    {
        VerifyNotStarted();

        StartConsumer();

        _subscription = _observable.Subscribe(item => ItemsQueue.Enqueue(item));
        _started = true;
    }

    public override async Task Watch(Func<bool> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        while (true)
        {
            await Wait(cancellationToken);
            if (predicate())
                return;
        }
    }

    public override Task Wait(CancellationToken cancellationToken = default)
    {
       return ItemReceivedEvent.Wait(cancellationToken);
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

    protected virtual void StartConsumer()
    {
        // We need to publish items on the given scheduler which should be the strategy's main thread.
        TaskExtensions.Run(Publish, PublishScheduler).Forget();
    }

    protected virtual async Task Publish()
    {
        try
        {
            while (true)
            {
                var item = await ItemsQueue.DequeueAsync(_disposeCts.Token);

                try
                {
                    ItemHandler(item);
                    ItemReceivedEvent.Set();
                }
                catch { /* NOP */ }
            }
        }
        catch (OperationCanceledException) { }
    }
}