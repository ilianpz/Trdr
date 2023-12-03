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
    private readonly AsyncLock _observableMutex = new();
    private readonly AsyncLock _observersMutex = new();
    private readonly IAsyncEnumerable<T> _stream;

    private HashSet<Observer>? _observers = new();
    private Exception? _lastError;
    
    internal Watcher(IAsyncEnumerable<T> stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        ConsumeStream().Forget();
    }

    /// <summary>
    /// Waits until <paramref name="predicate"/> is true.
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns>
    /// Awaited task is true if <paramref name="predicate"/> returned true. False if the stream completed.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    public Task<bool> Watch(Func<T?, bool> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        using (_observableMutex.Lock())
        using (_observersMutex.Lock())
        {
            if (_observers == null)
            {
                // Handle the case where Wait is called after the stream has ended.
                // Otherwise, the caller will await forever.
                
                // We had an error when the stream ended. We need to rethrow.
                if (_lastError != null)
                    ExceptionDispatchInfo.Capture(_lastError).Throw();

                // The stream ended gracefully. Just return false.
                return Task.FromResult(false);
            }
            
            var tcs = new TaskCompletionSource<bool>();
            _observers.Add(new Observer(predicate, tcs));
            return tcs.Task;
        }
    }
    
    private async Task ConsumeStream()
    {
        try
        {
            Debug.Assert(_observers != null, nameof(_observers) + " != null");

            await foreach (T item in _stream.ConfigureAwait(false))
            {
                using (await _observersMutex.LockAsync())
                {
                    // Need to copy _observers (via ToList()) to avoid modifying and
                    // enumerating the same collection.
                    // TODO: Check performance later. Use LinkedList instead?
                    foreach (var observer in _observers.ToList())
                    {
                        if (observer.Predicate(item))
                        {
                            observer.Tcs.SetResult(true);
                            _observers.Remove(observer);
                        }
                    }
                }
            }

            using (await _observableMutex.LockAsync())
            using (await _observersMutex.LockAsync())
            {
                // The stream ended gracefully
                foreach (var observer in _observers.ToList())
                {
                    observer.Tcs.SetResult(false);
                }

                _observers = null;
            }
        }
        catch (Exception ex)
        {
            using (await _observableMutex.LockAsync())
            using (await _observersMutex.LockAsync())
            {
                foreach (var observer in _observers!.ToList())
                {
                    observer.Tcs.SetException(ex);
                }

                _observers = null;
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
}