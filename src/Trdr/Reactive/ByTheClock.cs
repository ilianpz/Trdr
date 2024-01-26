using Trdr.Windows;

namespace Trdr.Reactive;

public static partial class ObservableExtensions
{
    public static IObservable<DateTime> ByTheClock(TimeSpan interval)
    {
        return new ByTheClockObservable(interval);
    }
}

public static class ByTheClock
{
    public static TimeSpan GetDelayForInterval(TimeSpan interval, out DateTime targetTime)
    {
        return GetDelayForInterval(interval, UtcNow, out targetTime);

        DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }
    }

    internal static TimeSpan GetDelayForInterval(TimeSpan interval, Func<DateTime> getUtcNow, out DateTime targetTime)
    {
        var now = getUtcNow();
        var time = now.TimeOfDay;
        var delay = interval - TimeSpan.FromMilliseconds((time + interval).TotalMilliseconds % interval.TotalMilliseconds);
        targetTime = now.Date + (time + delay).Round(interval);
        return delay;
    }
}

/// <summary>
/// Defines an observable that emits the time every interval by the clock. For example, if a 1-second interval
/// is specified and the current time is 12:01:03, the observable will emit 12:01:04, 12:01:05, 12:01:06, etc.
/// </summary>
public sealed class ByTheClockObservable : IObservable<DateTime>
{
    private readonly TimeSpan _interval;

    public ByTheClockObservable(TimeSpan interval)
    {
        _interval = interval;
    }

    public IDisposable Subscribe(IObserver<DateTime> observer)
    {
        var sleep = new HighResolutionSleep();
        var thread = new Thread(_ => OnStart((observer, sleep)))
        {
            IsBackground = true
        };

        thread.Start();
        return sleep;
    }

    private void OnStart(object? arg)
    {
        var (observer, sleep) = ((IObserver<DateTime>, HighResolutionSleep))arg!;

        try
        {
            var delay = ByTheClock.GetDelayForInterval(_interval, out var targetTime);

            while (true)
            {
                sleep.Wait(delay);
                observer.OnNext(targetTime);

                // This ensures that we don't miss any interval. If this thread is slow, this should
                // allow catching up because no delay will be applied (i.e., the value is less
                // than or equal to zero).
                targetTime += _interval;
                delay = targetTime - DateTime.UtcNow;
            }
        }
        catch
        {
            // We don't care about errors so just complete the stream.
            observer.OnCompleted();
        }
    }
}