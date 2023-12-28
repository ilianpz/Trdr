using System.Reactive.Subjects;
using Nito.AsyncEx;

namespace Trdr.Tests;

public sealed class SentinelTests
{
    [Test]
    public async Task Can_watch()
    {
        var subject = new Subject<int>();

        int value1 = 0;
        var sentinel = Sentinel.Create(subject, item => value1 = item.Value);
        sentinel.Start();

        subject.OnNext(1);

        await sentinel.Watch(() => value1 == 1);
        Assert.That(value1, Is.EqualTo(1));
    }

    [Test]
    public async Task Can_combine_2()
    {
        var subject1 = new Subject<int>();
        var subject2 = new Subject<int>();

        int value1 = 0;
        int value2 = 0;
        var sentinel =
            Sentinel.Create(subject1, item => value1 = item.Value)
                .Combine(subject2, item => value2 = item.Value);

        sentinel.Start();

        subject1.OnNext(1);
        subject2.OnNext(2);

        await sentinel.Watch(() => value2 == 2);

        Assert.That(value1, Is.EqualTo(1));
        Assert.That(value2, Is.EqualTo(2));
    }

    [Test]
    public async Task Can_combine_3()
    {
        var subject1 = new Subject<int>();
        var subject2 = new Subject<int>();
        var subject3 = new Subject<int>();

        var publishEvent = new AsyncAutoResetEvent();

        int value1 = 0;
        int value2 = 0;
        int value3 = 0;
        var sentinel =
            Sentinel.Create(subject1, item => value1 = item.Value)
                .Combine(subject2, item => value2 = item.Value)
                .Combine(subject3,
                    item =>
                    {
                        value3 = item.Value;
                        publishEvent.Set();
                    });

        sentinel.Start();

        subject1.OnNext(1);
        subject2.OnNext(2);
        subject3.OnNext(3);

        await sentinel.Watch(() => value3 == 3);

        Assert.That(value1, Is.EqualTo(1));
        Assert.That(value2, Is.EqualTo(2));
        Assert.That(value3, Is.EqualTo(3));
    }

    [Test]
    public void Exception_in_handler_is_bubble_up()
    {
        var subject = new Subject<int>();

        var sentinel = Sentinel.Create(subject, item => throw new InvalidOperationException());
        sentinel.Start();

        subject.OnNext(1);

        Assert.That(async () => await sentinel.Watch(() => true), Throws.InvalidOperationException);
    }
}