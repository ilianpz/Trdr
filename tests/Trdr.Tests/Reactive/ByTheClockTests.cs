using System.Reactive.Linq;
using ObservableExtensions = Trdr.Reactive.ObservableExtensions;

namespace Trdr.Tests.Reactive;

public sealed class ByTheClockTests
{
    [Test]
    public void Does_not_miss_intervals()
    {
        var interval = TimeSpan.FromMilliseconds(10);

        var times =
            ObservableExtensions.ByTheClock(interval)
                .Do(_ => Thread.Sleep(TimeSpan.FromMilliseconds(10)))   // Add a delay
                .Take(10)
                .ToEnumerable()
                .ToList();

        for (int i = 1; i < times.Count; ++i)
        {
            Assert.That(times[i] - times[i - 1], Is.EqualTo(interval));
        }
    }
}