using System.Reactive.Linq;
using System.Reactive.Subjects;
using Nito.AsyncEx;

namespace Trdr;

public sealed class Sentinel
{
    private readonly Subject<int> _signal = new();
    private readonly CancellationTokenSource _stopCts = new();
    private readonly AsyncCountdownEvent _startEvent = new(0);
    private readonly AsyncAutoResetEvent _hasNextEvent = new(false);

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
        VerifyStartStop();

        // Consume the stream only when Start() has been called.
        var connectable = observable.Publish();

        // Count all subscriptions and signal the first event from each subscription.
        // This will enable us to signal that we have started only when each
        // subscription has emitted its first item.
        _startEvent.AddCount(1);
        connectable.FirstAsync().Subscribe(_ => _startEvent.Signal());

        var subscription = connectable.Subscribe(
            item =>
            {
                lock (_signal)
                {
                    // Because multiple streams can have different thread contexts. We
                    // need to synchronize calling the handler so user won't have to do it
                    // themselves.
                    handler(item);
                }

                // Any time we got an item from one of our source streams, we need to
                // evaluate all of our sentinel predicates.
                _signal.OnNext(0);
            });
        _stopCts.Token.Register(() => subscription.Dispose());

        _start += () =>
        {
            // Start the stream.
            //
            // Connect isn't defined on a base1 non-generic interface. So we use a delegate here
            // as a workaround to call instances of different types when Start() is called.
            connectable.Connect();
        };
    }

    /// <summary>
    /// Starts all subscriptions.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that continues after each subscription has emitted its first item.
    /// </returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Task Start()
    {
        VerifyStartStop();

        _start();
        _started = true;

        return _startEvent.WaitAsync();
    }

    public void Stop()
    {
        if (_stopped)
            return;

        _stopCts.Cancel();
        _stopCts.Dispose();
        _stopped = true;
    }

    public async Task<bool> NextEvent(CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(
            _stopCts.Token, cancellationToken);

        try
        {
            await _hasNextEvent.WaitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            if (_stopCts.IsCancellationRequested)
                return false;

            throw;
        }
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
        if (_stopped) throw new InvalidOperationException("This instance has already stopped.");

        if (predicate())
            return;

        TaskCompletionSource tcs = new();
        var subscription = _signal.Subscribe(
            _ =>
            {
                try
                {
                    if (predicate())
                        tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

        using (subscription)
        await using (_stopCts.Token.Register(OnCanceled).ConfigureAwait(false))
        await using (cancellationToken.Register(OnCanceled))
        {
            await tcs.Task;
        }

        return;

        void OnCanceled()
        {
            tcs.TrySetCanceled(cancellationToken);
        }
    }

    private void VerifyStartStop()
    {
        if (_started) throw new InvalidOperationException("This instance has already started.");
        if (_stopped) throw new InvalidOperationException("This instance has already stopped.");
    }
}