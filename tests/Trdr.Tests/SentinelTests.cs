using System.Reactive.Subjects;

namespace Trdr.Tests;

public class SentinelTests
{
    [Test]
    public async Task Can_combine_2()
    {
        var subject1 = new Subject<int>();
        var subject2 = new Subject<int>();

        int value1 = 0;
        int value2 = 0;
        var @event =
            Sentinel.Create(subject1, item => value1 = item.Value, TaskScheduler.Default)
                .Combine(subject2, item => value2 = item.Value);

        @event.Start();

        subject1.OnNext(1);
        subject2.OnNext(2);

        await @event.Wait();

        Assert.That(value1, Is.EqualTo(1));
        Assert.That(value2, Is.EqualTo(2));
    }

    [Test]
    public async Task Can_combine_3()
    {
        var subject1 = new Subject<int>();
        var subject2 = new Subject<int>();
        var subject3 = new Subject<int>();

        int value1 = 0;
        int value2 = 0;
        int value3 = 0;
        var @event =
            Sentinel.Create(subject1, item => value1 = item.Value, TaskScheduler.Default)
                .Combine(subject2, item => value2 = item.Value)
                .Combine(subject3, item => value3 = item.Value);

        @event.Start();

        subject1.OnNext(1);
        subject2.OnNext(2);
        subject3.OnNext(3);

        await @event.Wait();

        Assert.That(value1, Is.EqualTo(1));
        Assert.That(value2, Is.EqualTo(2));
        Assert.That(value3, Is.EqualTo(3));
    }
}