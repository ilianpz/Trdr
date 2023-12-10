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

        Assert.That(async () => await watchTask, Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void Watch_can_throw()
    {
        var signal = new Subject<int>();
        var sentinel = new Sentinel();
        sentinel.Subscribe(signal, delegate { });
        sentinel.Start();

        var watchTask = sentinel.Watch(() => throw new Exception("ThisInstance"));

        signal.OnNext(1);

        Assert.That(
            async () =>  await watchTask,
            Throws.InstanceOf<Exception>().With.Message.EqualTo("ThisInstance"));
    }

    [Test]
    public void Can_stop()
    {
        var sentinel = new Sentinel();
        sentinel.Start().Forget();

        Task neverEndingWatch = sentinel.Watch(() => false);
        sentinel.Stop();

        Assert.That(async () =>  await neverEndingWatch, Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void Next_event_returns_false_when_stopped()
    {
        var sentinel = new Sentinel();
        sentinel.Start().Forget();

        Task<bool> nextEvent = sentinel.NextEvent();
        sentinel.Stop();

        Assert.That(async () =>  await nextEvent, Is.False);
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