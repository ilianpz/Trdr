using System.Reactive.Subjects;

namespace Trdr.Tests;

public sealed class SentinelTests
{
    [Test]
    public void Watch_can_succeed()
    {
        var sentinel = new Sentinel();
        var subject = new Subject<int>();

        int value = 0;
        sentinel.Subscribe(subject, i => value = i);
        sentinel.Start();

        Task watchTask = sentinel.Watch(() => value == 2);

        subject.OnNext(1);
        Assert.That(() =>  watchTask.IsCompleted, Is.False);
        subject.OnNext(2);
        Assert.That(() =>  watchTask.IsCompleted);
    }

    [Test]
    public void Watch_can_succeed_with_already_published_value()
    {
        var sentinel = new Sentinel();
        var subject = new Subject<int>();

        int value = 0;
        sentinel.Subscribe(subject, i => value = i);
        sentinel.Start();

        subject.OnNext(1);
        Assert.That(() =>  sentinel.Watch(() => value == 1).IsCompleted);
    }

    [Test]
    public void Watch_can_be_canceled()
    {
        var sentinel = new Sentinel();
        var subject = new Subject<int>();

        int value = 0;
        sentinel.Subscribe(subject, i => value = i);
        sentinel.Start();

        var cts = new CancellationTokenSource();
        Task watchTask = sentinel.Watch(() => value == 2, cts.Token);

        subject.OnNext(1);
        Assert.That(watchTask.IsCompleted, Is.False);

        cts.Cancel();
        subject.OnNext(2);

        Assert.That(async () => await watchTask, Throws.InstanceOf<TaskCanceledException>());
    }

    [Test]
    public void Watch_can_throw()
    {
        var signal = new Subject<int>();
        var sentinel = new Sentinel();
        sentinel.Subscribe(signal, delegate { });
        sentinel.Start();

        signal.OnNext(1);

        Assert.That(
            () =>  sentinel.Watch(() => throw new Exception("ThisInstance")),
            Throws.InstanceOf<Exception>().With.Message.EqualTo("ThisInstance"));
    }

    [Test]
    public void Can_stop()
    {
        var sentinel = new Sentinel();
        sentinel.Start();

        Task neverEndingWatch = sentinel.Watch(() => false);
        sentinel.Stop();

        Assert.That(async () =>  await neverEndingWatch, Throws.InstanceOf<TaskCanceledException>());
    }

    [Test]
    public void Watch_supports_async_enumerable()
    {
        var sentinel = new Sentinel();
        var subject = new Subject<int>();

        int value = 0;
        sentinel.Subscribe(subject.ToAsyncEnumerable(), i => value = i);
        sentinel.Start();

        Task watchTask = sentinel.Watch(() => value == 2);

        subject.OnNext(1);
        Assert.That(() =>  watchTask.IsCompleted, Is.False);
        subject.OnNext(2);
        Assert.That(() =>  watchTask.IsCompleted);
    }
}