using System.Reactive.Concurrency;

namespace Trdr.Reactive;

internal sealed class SubscribeLatestHandler : SubscribeHandler
{
    private Func<Task>? _latestHandler;
    private bool _isHandling;

    public SubscribeLatestHandler(IScheduler scheduler) : base(scheduler)
    {
    }

    public Task Subscribe<T>(IObservable<T> stream, Func<T, Task> onHandleItem, CancellationToken cancellationToken)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (onHandleItem == null) throw new ArgumentNullException(nameof(onHandleItem));

        return SubscribeCore(stream, onHandleItem, cancellationToken);
    }

    protected override async Task GuardHandler(Func<Task> onHandleItem)
    {
        // IObservable.Subscribe does not "block" onNext if handleItem is awaiting something. So this method
        // can be called concurrently. If the current handleItem does not complete quickly, we need to prevent
        // the next handleItem from running concurrently.
        // Store the latest handler and run it later. Note, this will mean that we will drop stale updates.
        _latestHandler = onHandleItem;

        // If there is an existing handler running, exit early.
        if (_isHandling)
            return;

        try
        {
            _isHandling = true;

            do
            {
                var currentHandler = _latestHandler;
                _latestHandler = null;

                await currentHandler();

                // While awaiting above, the stream can emit a new item which will be stored in _latestHandler.
                // If we have one, run that too.
            } while (_latestHandler != null);
        }
        finally
        {
            _isHandling = false;
        }
    }
}