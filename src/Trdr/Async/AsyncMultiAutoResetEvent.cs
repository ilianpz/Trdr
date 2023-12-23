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
            if (Interlocked.CompareExchange(ref _releasingLockTaken, 1, 0) == 0)
            {
                await _lock.WaitAsync().ConfigureAwait(false);
                lockTaken = true;
            }
        }
        finally
        {
            Interlocked.Decrement(ref _waitingCount);

            if (lockTaken)
            {
                // Spin until all awaiters have been released.
                while (Interlocked.Read(ref _waitingCount) != 0)
                {
                    await Task.Yield();
                }

                _event.Reset();
                Interlocked.CompareExchange(ref _releasingLockTaken, 0, 1);
                _lock.Release();
            }
        }
    }
}