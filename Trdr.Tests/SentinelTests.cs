using System.Reactive.Subjects;

namespace Trdr.Tests;

public class SentinelTests
{
    [Test]
    public async Task Can_combine_2()
    {
        var subject1 = new Subject<int>();
        var subject2 = new Subject<int>();

        int item1 = 0;
        int item2 = 0;
        var @event = Sentinel<int>.Create(subject1, item => item1 = item, TaskScheduler.Default)
            .Combine(subject2, item => item2 = item);

        @event.Start();

        subject1.OnNext(1);
        subject2.OnNext(2);

        await @event.Wait();

        Assert.That(item1, Is.EqualTo(1));
        Assert.That(item2, Is.EqualTo(2));
    }

    [Test]
    public async Task Can_combine_3()
    {
        var subject1 = new Subject<int>();
        var subject2 = new Subject<int>();
        var subject3 = new Subject<int>();

        int item1 = 0;
        int item2 = 0;
        int item3 = 0;
        var @event = Sentinel<int>.Create(subject1, item => item1 = item, TaskScheduler.Default)
            .Combine(subject2, item => item2 = item)
            .Combine(subject3, item => item3 = item);

        @event.Start();

        subject1.OnNext(1);
        subject2.OnNext(2);
        subject3.OnNext(3);

        await @event.Wait();

        Assert.That(item1, Is.EqualTo(1));
        Assert.That(item2, Is.EqualTo(2));
        Assert.That(item3, Is.EqualTo(3));
    }
}