using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using Trdr.Reactive;

namespace Trdr.Tests.Reactive;

public sealed class SubscribeLatestHandlerTests
{
    [Test]
    public void Can_handle_latest_updates()
    {
        var subject = new Subject<int>();
        var updates = new List<int>();

        var handler = new SubscribeLatestHandler(ImmediateScheduler.Instance);
        var tcs = new TaskCompletionSource();
        handler.Subscribe(
            subject,
            item =>
            {
                updates.Add(item);
                return tcs.Task;
            },
            CancellationToken.None);

        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);

        tcs.SetResult();

        subject.OnNext(4);

        Assert.That(updates, Is.EqualTo(new[] { 1, 3, 4 }));
    }
}