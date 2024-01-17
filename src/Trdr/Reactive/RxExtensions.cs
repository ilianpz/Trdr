using System.Reactive;

namespace Trdr.Reactive;

public static class RxExtensions
{
    public static Timestamped<T> Timestamp<T>(this T item)
    {
        return new Timestamped<T>(item, DateTimeOffset.UtcNow);
;    }
}