using Nito.Disposables;

namespace Trdr.Reactive;

public static partial class ObservableExtensions
{
    public static IObservable<(T1, T2)> ZipWithLatest<T1, T2>(
        this IObservable<T1> observable1, IObservable<T2> observable2)
    {
        return new ZipWithLatest<T1, T2>(observable1, observable2);
    }
}

/// <summary>
/// Defines an operator that zips two streams but always takes the latest value from each stream.
///
/// For example:
///
/// stream1 ---- 1 ----- 2 ---------------- 3 ------
/// stream2 -------------------- a --- b------------
/// result  ----------------- (2, a)------ (3, b) --
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public sealed class ZipWithLatest<T1, T2> : IObservable<(T1, T2)>
{
    private readonly IObservable<T1> _observable1;
    private readonly IObservable<T2> _observable2;

    public ZipWithLatest(IObservable<T1> observable1, IObservable<T2> observable2)
    {
        _observable1 = observable1 ?? throw new ArgumentNullException(nameof(observable1));
        _observable2 = observable2 ?? throw new ArgumentNullException(nameof(observable2));
    }

    public IDisposable Subscribe(IObserver<(T1, T2)> observer)
    {
        var itemHolder1 = new ItemHolder<T1>();
        var itemHolder2 = new ItemHolder<T2>();

        var subscription1 = SubscribeImpl(_observable1, itemHolder1, itemHolder2);
        var subscription2 = SubscribeImpl(_observable2, itemHolder2, itemHolder1);

        return Disposable.Create(
            () =>
            {
                subscription1.Dispose();
                subscription2.Dispose();
            });

        void Publish()
        {
            observer.OnNext((itemHolder1.Item, itemHolder2.Item));
            itemHolder1.Reset();
            itemHolder2.Reset();
        }

        IDisposable SubscribeImpl<TThis, TOther>(IObservable<TThis> observable,
            ItemHolder<TThis> thisHolder,
            ItemHolder<TOther> otherHolder)
        {
            return observable.Subscribe(
                item =>
                {
                    thisHolder.SetItem(item);
                    if (otherHolder.TryGetItem(out _))
                        Publish();
                },
                observer.OnError,
                observer.OnCompleted);
        }
    }
}