using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Trdr.Reactive;

internal abstract class SubscribeHandler
{
    private readonly IScheduler _scheduler;

    protected SubscribeHandler(IScheduler scheduler)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    protected abstract Task GuardHandler(Func<Task> onHandleItem);

    protected async Task SubscribeCore<T>(
        IObservable<T> stream, Func<T, Task> onHandleItem,
        CancellationToken cancellationToken)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (onHandleItem == null) throw new ArgumentNullException(nameof(onHandleItem));

        var tcs = new TaskCompletionSource();
        stream
            .ObserveOn(_scheduler)
            .Subscribe(
                item => GuardHandler(() => onHandleItem(item)),
                onError: ex => tcs.TrySetException(ex),
                onCompleted: () => tcs.TrySetResult());

        await using var cancellation = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        await tcs.Task;
    }
}