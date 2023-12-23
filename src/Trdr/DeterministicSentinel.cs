using Trdr.Reactive;
using Trdr.Windows;

namespace Trdr;

/// <summary>
/// Defines a time-triggered deterministic <see cref="Sentinel{T}"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class DeterministicSentinel<T> : Sentinel<T>
{
    internal DeterministicSentinel(IObservable<T> observable, Action<Timestamped<T>> handler, TaskScheduler scheduler)
        : base(observable, handler, scheduler)
    {
    }

    public override Sentinel Combine<TOther>(IAsyncEnumerable<TOther> enumerable,
        Action<Timestamped<TOther>> handler)
    {
        throw new NotSupportedException();
    }

    public override Sentinel Combine<TOther>(IObservable<TOther> observable, Action<Timestamped<TOther>> handler)
    {
        // A deterministic sentinel can no longer combine with other sentinels.
        throw new NotSupportedException();
    }

    protected override void StartConsumer()
    {
        var thread = new Thread(OnStart) { IsBackground = true };
        thread.Start();
    }

    private void OnStart()
    {
        using var sleep = new HighResolutionSleep();

        while (true)
        {
            sleep.Wait(
                ByTheClock.GetDelayForInterval(
                    TimeSpan.FromMilliseconds(15),
                    out _));
        }
    }
}