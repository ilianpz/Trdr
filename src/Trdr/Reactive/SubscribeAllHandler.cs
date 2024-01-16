using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Trdr.Reactive;

internal sealed class SubscribeAllHandler : SubscribeHandler
{
    public SubscribeAllHandler(IScheduler scheduler) : base(scheduler)
    {
    }

    public Task Subscribe<T>(IObservable<T> stream, Action<T> onHandleItem, CancellationToken cancellationToken)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (onHandleItem == null) throw new ArgumentNullException(nameof(onHandleItem));

        return SubscribeCore(
            stream,
            item =>
            {
                onHandleItem(item);
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    protected override Task GuardHandler(Func<Task> onHandleItem)
    {
        return onHandleItem();
    }
}