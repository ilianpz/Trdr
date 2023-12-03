using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Nito.AsyncEx;

namespace Trdr;

public static class Watcher
{
    public static Watcher<T> Create<T>(IAsyncEnumerable<T> stream)
    {
        return new Watcher<T>(stream);
    }
}

public sealed class Watcher<T>
{
    private readonly AsyncLock _observersMutex = new();
    private readonly IAsyncEnumerable<T> _stream;
    private readonly ItemHolder _lastUnobservedItem = new();

    private HashSet<Observer>? _observers;
    private bool _isCompleted;
    private Exception? _lastError;
    
    internal Watcher(IAsyncEnumerable<T> stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    /// <summary>
    /// Waits until <paramref name="predicate"/> is true.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// Awaited task is true if <paramref name="predicate"/> returned true. False if the stream completed.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<bool> Watch(Func<T?, bool> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        TaskCompletionSource<bool> tcs;
        using (await _observersMutex.LockAsync().ConfigureAwait(false))
        {
            if (_observers == null)
            {
                // Only start enumerating the stream the first time Watch is called.
                _observers = new HashSet<Observer>();
                EnumerateStream().Forget();
            }

            if (_lastUnobservedItem.TryGet(out var item))
            {
                _lastUnobservedItem.Reset();

                if (predicate(item))
                    return true;
            }

            if (_isCompleted)
            {
                // Handle the case where Watch is called after the stream has ended.
                // Otherwise, the caller will await forever.
                
                // We had an error when the stream ended. We need to rethrow.
                if (_lastError != null)
                    ExceptionDispatchInfo.Capture(_lastError).Throw();

                // The stream ended gracefully. Just return false.
                return false;
            }
            
            tcs = new TaskCompletionSource<bool>();
            _observers.Add(new Observer(predicate, tcs));
        }

        await using (cancellationToken.Register(() => tcs.TrySetCanceled()))
        {
            return await tcs.Task;
        }
    }
    
    private async Task EnumerateStream()
    {
        try
        {
            Debug.Assert(_observers != null, nameof(_observers) + " != null");

            await foreach (T item in _stream.ConfigureAwait(false))
            {
                using (await _observersMutex.LockAsync())
                {
                    _lastUnobservedItem.Set(item);

                    // Need to copy _observers (via ToList()) to avoid modifying and
                    // enumerating the same collection.
                    // TODO: Check performance later. Use LinkedList instead?
                    foreach (var observer in _observers.ToList())
                    {
                        if (observer.Predicate(item))
                        {
                            observer.Tcs.TrySetResult(true);
                            _observers.Remove(observer);
                        }

                        _lastUnobservedItem.Reset();
                    }
                }
            }

            using (await _observersMutex.LockAsync())
            {
                // The stream ended gracefully
                foreach (var observer in _observers.ToList())
                {
                    observer.Tcs.TrySetResult(false);
                }

                _isCompleted = true;
            }
        }
        catch (Exception ex)
        {
            using (await _observersMutex.LockAsync())
            {
                foreach (var observer in _observers!.ToList())
                {
                    observer.Tcs.TrySetException(ex);
                }

                _isCompleted = true;
                _lastError = ex;
            }
        }
    }

    private sealed class Observer
    {
        public Observer(Func<T, bool>  predicate, TaskCompletionSource<bool> tcs)
        {
            Predicate = predicate;
            Tcs = tcs;
        }
        
        public Func<T, bool> Predicate { get; }
        public TaskCompletionSource<bool> Tcs { get; }
    }

    // Helper class to indicate if an item is present. This solves the case
    // where a null instance is a valid item.
    private sealed class ItemHolder
    {
        private bool _hasItem;
        private T? _item;

        public bool TryGet(out T? item)
        {
            item = _item;
            return _hasItem;
        }

        public void Set(T item)
        {
            _item = item;
            _hasItem = true;
        }

        public void Reset()
        {
            _item = default;
            _hasItem = false;
        }
    }
}