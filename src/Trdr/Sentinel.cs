using Nito.AsyncEx;
using Trdr.Async;

namespace Trdr;

public static class Sentinel
{
    public static Sentinel<T> Create<T>(IAsyncEnumerable<T> enumerable, Action<Timestamped<T>> handler,
        TaskScheduler? taskScheduler = null)
    {
        return Create(enumerable.ToObservable(), handler, taskScheduler);
    }

    public static Sentinel<T> Create<T>(IObservable<T> observable, Action<Timestamped<T>> handler,
        TaskScheduler? taskScheduler = null)
    {
        return new Sentinel<T>(observable, handler, taskScheduler);
    }
}

public sealed class Sentinel<T> : IDisposable
{
    private readonly IObservable<T> _observable;
    private readonly Action<Timestamped<T>> _handler;
    private readonly TaskScheduler _taskScheduler;
    private readonly TaskCompletionSource _completionTcs = new();

    private readonly Queue<T> _queue = new();
    private readonly AsyncAutoResetEvent _receivedItemsEvent = new();
    private readonly AsyncMultiAutoResetEvent _handledItemsEvent = new(false);

    private List<PredicateHolder> _predicates = new();
    private CancellationTokenSource? _disposeCts = new();
    private IDisposable? _baseSubscription;

    public Sentinel(IObservable<T> observable, Action<Timestamped<T>> handler, TaskScheduler? taskScheduler)
    {
        _observable = observable ?? throw new ArgumentNullException(nameof(observable));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _taskScheduler = taskScheduler ?? TaskScheduler.Default;
    }

    public void Start()
    {
        TaskEx.Run(HandleItems, _taskScheduler).Forget();

        _baseSubscription = _observable.Subscribe(
            onNext: item =>
            {
                _queue.Enqueue(item);
                _receivedItemsEvent.Set();
            },
            onError: ex => _completionTcs.TrySetException(ex),
            onCompleted: () => _completionTcs.TrySetResult());
    }

    public void Dispose()
    {
        if (_disposeCts != null)
        {
            _disposeCts.Cancel();
            _disposeCts = null;
        }

        _baseSubscription?.Dispose();
    }

    public async Task<bool> Watch(Func<bool> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        await _handledItemsEvent.Wait(cancellationToken);

        var completionTask = _completionTcs.Task;
        if (completionTask.IsCompleted)
            return await HandleCompletedSubscription();

        if (predicate())
            return true;

        var tcs = new TaskCompletionSource();
        _predicates.Add(new PredicateHolder { Predicate = predicate, Tcs = tcs });

        // Check which task finished first
        var predicateTask = tcs.Task;
        var firstTask = await Task.WhenAny(completionTask, predicateTask);
        if (firstTask == completionTask)
            return await HandleCompletedSubscription();

        return true;

        async Task<bool> HandleCompletedSubscription()
        {
            // This will only complete if _observable has an error or has ended.
            // In case of error, throw here...
            await completionTask;
            // In case _observable ended, end here and indicate that the subscription has finished.
            return false;
        }
    }

    private async Task HandleItems()
    {
        var cancellationToken = _disposeCts!.Token;

        try
        {
            while (true)
            {
                await _receivedItemsEvent.WaitAsync(cancellationToken);

                bool hasHandled = false;
                while (_queue.Count > 0)
                {
                    var item = _queue.Dequeue();
                    _handler(Timestamped.Create(DateTime.UtcNow, item));
                    hasHandled = true;
                }

                if (!hasHandled)
                    continue;

                var pendingPredicates = new List<PredicateHolder>();
                foreach (var predicateHolder in _predicates)
                {
                    if (predicateHolder.Predicate())
                    {
                        predicateHolder.Tcs.TrySetResult();
                        continue;
                    }

                    // This predicate is not true yet. Just re-add it to our list of predicates.
                    pendingPredicates.Add(predicateHolder);
                }

                _predicates = pendingPredicates;

                _handledItemsEvent.Set();
                await Task.Yield(); // Allow any completed watchers to continue first
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _completionTcs.TrySetResult(); // End
        }
        catch (Exception ex)
        {
            _completionTcs.TrySetException(ex);
        }
        finally
        {
            _handledItemsEvent.Set();
            await Task.Yield(); // Allow any completed watchers to continue first
        }
    }

    private record struct PredicateHolder
    {
        public required Func<bool> Predicate { get; init; }
        public required TaskCompletionSource Tcs { get; init; }
    }
}