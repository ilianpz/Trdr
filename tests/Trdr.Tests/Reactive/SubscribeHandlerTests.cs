using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using Trdr.Reactive;

namespace Trdr.Tests.Reactive;

public sealed class SubscribeAllHandlerTests
{
    [Test]
    public void Can_handle_all_updates()
    {
        var subject = new Subject<int>();
        var updates = new List<int>();

        var handler = new SubscribeAllHandler(ImmediateScheduler.Instance);
        handler.Subscribe(subject, item => updates.Add(item), CancellationToken.None);

        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);

        Assert.That(updates, Is.EqualTo(new[] { 1, 2, 3 }));
    }
}