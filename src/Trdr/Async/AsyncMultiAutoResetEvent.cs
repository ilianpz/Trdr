using Nito.AsyncEx;

namespace Trdr.Async;

/// <summary>
/// Represents an asynchronous auto-reset event that can release multiple awaiting threads.
/// </summary>
public sealed class AsyncMultiAutoResetEvent
{
    private readonly bool _asyncCompletion;
    private readonly object _mutex = new();
    private List<TaskCompletionSource> _tcsList = new();
    private bool isSet;

    public AsyncMultiAutoResetEvent(bool asyncCompletion = true)
    {
        _asyncCompletion = asyncCompletion;
    }

    public void Set()
    {
        List<TaskCompletionSource> tcsList;
        lock (_mutex)
        {
            tcsList = _tcsList;
            _tcsList = new();

            // If there are no awaiters, we have to leave this event as set.
            isSet = tcsList.Count == 0;
        }

        // We complete the awaiting tasks outside the lock in case the user wants
        // synchronous completion. Otherwise, we might block the thread.
        foreach (var tcs in tcsList)
        {
            tcs.TrySetResult();
        }
    }

    public async Task Wait(CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource(_asyncCompletion ? TaskCreationOptions.RunContinuationsAsynchronously : 0);
        lock (_mutex)
        {
            if (isSet)
            {
                // This is the first awaiter for an already set event. Just return early and reset the event.
                isSet = false;
                return;
            }

            _tcsList.Add(tcs);
        }

        await using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)).ConfigureAwait(false))
        {
            await tcs.Task.ConfigureAwait(false);
        }
    }
}