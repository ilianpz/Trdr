using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Trdr;

public class Sentinel
{
    private readonly Subject<int> _signal = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly HashSet<PredicatePair> _predicates = new();

    private bool _hasUnobservedSignal;
    private bool _started;
    private bool _stopped;
    private Action _start = delegate {  };

    /// <summary>
    /// Subscribes to the give <paramref name="enumerable"/>.
    /// </summary>
    /// <param name="enumerable"></param>
    /// <param name="handler"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    /// Note, this only registers <paramref name="handler"/> to the given <paramref name="enumerable"/>.
    /// <see cref="Start"/> still has to be called in order to start the subscription.
    /// </remarks>
    public void Subscribe<T>(IAsyncEnumerable<T> enumerable, Action<T> handler)
    {
        if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

        Subscribe(enumerable.ToObservable(), handler);
    }

    /// <summary>
    /// Subscribes to the give <paramref name="observable"/>.
    /// </summary>
    /// <param name="observable"></param>
    /// <param name="handler"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    /// Note, this only registers <paramref name="handler"/> to the given <paramref name="observable"/>.
    /// <see cref="Start"/> still has to be called in order to start the subscription.
    /// </remarks>
    public void Subscribe<T>(IObservable<T> observable, Action<T> handler)
    {
        if (observable == null) throw new ArgumentNullException(nameof(observable));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        // Consume the stream only when Start() has been called.
        var connectable = observable.Publish();

        var subscription = connectable.Subscribe(
            item =>
            {
                lock (_signal)
                {
                    // Because multiple streams can have different thread contexts. We
                    // need to synchronize here so callers won't have to do it themselves.
                    handler(item);
                }

                // Any time we got an item from one of our source streams, we need to
                // evaluate all of our sentinel predicates.
                _signal.OnNext(0);
            });
        _cancellationTokenSource.Token.Register(() => subscription.Dispose());

        _start += () =>
        {
            // Start the stream.
            //
            // Connect isn't defined on a base non-generic interface. So we use a delegate here
            // as a workaround to call instances of different types when Start() is called.
            connectable.Connect();
        };
    }

    /// <summary>
    /// Starts all subscriptions.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start()
    {
        if (_started) throw new InvalidOperationException("This instance has already started.");
        if (_stopped) throw new InvalidOperationException("This instance has already stopped.");

        var subscription = _signal.Subscribe(_ =>
        {
            List<PredicatePair> predicatesCopy;
            lock (_predicates)
            {
                // Copy to prevent modifying while enumerating.
                predicatesCopy = _predicates.ToList();
                _hasUnobservedSignal = predicatesCopy.Count == 0;
            }

            foreach (var predicatePair in predicatesCopy.ToList())
            {
                try
                {
                    bool isTrue;
                    lock (_signal)
                    {
                        // This signal can come from different thread contexts. We
                        // need to synchronize here so callers won't have to do it themselves.
                        isTrue = predicatePair.Predicate();
                    }

                    if (isTrue)
                    {
                        Remove(predicatePair);
                        predicatePair.Tcs.TrySetResult();
                    }
                }
                catch (Exception ex)
                {
                    Remove(predicatePair);
                    predicatePair.Tcs.TrySetException(ex);
                }
            }
        });
        _cancellationTokenSource.Token.Register(() => subscription.Dispose());

        _start();
        _started = true;
    }

    public void Stop()
    {
        if (_stopped)
            return;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _stopped = true;
    }

    /// <summary>
    /// Asynchronously waits for a given <paramref name="predicate"/> to be true.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task Watch(Func<bool> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (!_started) throw new InvalidOperationException("This instance has not started.");
        if (_stopped) throw new InvalidOperationException("This instance has already stopped.");

        lock (_predicates)
        {
            // If we have an unobserved signal. Check if the given predicate is true. If it is, just return fast.
            // No need to store it.
            if (_hasUnobservedSignal && predicate())
                return;
        }

        var tcs = new TaskCompletionSource();
        var predicatePair = new PredicatePair(predicate, tcs);
        lock (_predicates)
        {
            _predicates.Add(predicatePair);
        }

        await using (_cancellationTokenSource.Token.Register(OnCanceled).ConfigureAwait(false))
        await using (cancellationToken.Register(OnCanceled))
        {
            await tcs.Task;
        }

        void OnCanceled()
        {
            // The watch has been canceled. We need to remove the predicate.
           Remove(predicatePair);
            tcs.TrySetCanceled(cancellationToken);
        }
    }

    private void Remove(PredicatePair predicatePair)
    {
        lock (_predicates)
        {
            _predicates.Remove(predicatePair);
        }
    }

    private class PredicatePair
    {
        public PredicatePair(Func<bool> predicate, TaskCompletionSource tcs)
        {
            Predicate = predicate;
            Tcs = tcs;
        }

        public Func<bool> Predicate { get; }
        public TaskCompletionSource Tcs { get; }
    }
}