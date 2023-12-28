using Nito.AsyncEx;

namespace Trdr.Async;

/// <summary>
/// Represents an asynchronous auto-reset event that can release multiple awaiting threads.
/// </summary>
public sealed class AsyncMultiAutoResetEvent
{
    private readonly AsyncManualResetEventEx _event;

    // Use a semaphore instead of the standard lock/monitor because
    // we may need to lock and unlock in different threads.
    private readonly SemaphoreSlim _lock = new(1, 1);

    private long _waitingCount;
    private int _releasingLockTaken;

    public AsyncMultiAutoResetEvent(bool asyncCompletion = true)
    {
        _event = new AsyncManualResetEventEx(asyncCompletion);
    }

    public void Set()
    {
        using (_lock.Lock())
        {
            _event.Set();
        }
    }

    public async Task Wait(CancellationToken cancellationToken = default)
    {
        bool wasWaiting = false;

        try
        {
            Task waitTask;
            using (await _lock.LockAsync(cancellationToken))
            {
                waitTask = _event.Wait(cancellationToken);

                // Count the number of awaiters. We will only reset this event
                // when all awaiters have been signalled.
                Interlocked.Increment(ref _waitingCount);
                wasWaiting = true;
            }

            await waitTask.ConfigureAwait(false);
        }
        finally
        {
            // Only release if we were able to wait because
            // LockAsync can throw before waiting.
            if (wasWaiting)
                await ReleaseAllWaiting().ConfigureAwait(false);
        }
    }

    private async Task ReleaseAllWaiting()
    {
        bool lockTaken = false;
        try
        {
            // Safely check if this is the first awaiter that is to be released
            if (Interlocked.CompareExchange(ref _releasingLockTaken, 1, 0) == 0)
            {
                // If this is the first awaiter, lock so that any concurrent calls to Set or Wait is blocked.
                Task waitTask = _lock.WaitAsync();

                // Only mark that the lock was taken if we are certain that the WaitAsync call above
                // was successful.
                lockTaken = true;

                await waitTask.ConfigureAwait(false);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _waitingCount);

            if (lockTaken)
            {
                // If the lock was taken by this awaiter, then it is responsible for releasing the lock.

                // Spin until we know that all awaiters have been released.
                while (Interlocked.Read(ref _waitingCount) != 0)
                {
                    await Task.Yield();
                }

                // Reset our state
                _event.Reset();
                Interlocked.CompareExchange(ref _releasingLockTaken, 0, 1);
                _lock.Release();
            }
        }
    }
}